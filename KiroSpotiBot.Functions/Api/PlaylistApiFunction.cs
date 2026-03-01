using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using KiroSpotiBot.Core.Interfaces;
using KiroSpotiBot.ApiModels;
using KiroSpotiBot.Infrastructure.Repositories;
using KiroSpotiBot.Functions.Mappers;

namespace KiroSpotiBot.Functions.Api;

/// <summary>
/// API endpoints for playlist data access from the Blazor WebAssembly frontend.
/// </summary>
public class PlaylistApiFunction
{
    private readonly ILogger<PlaylistApiFunction> _logger;
    private readonly IGroupChatRepository _groupChatRepository;
    private readonly ITrackRecordRepository _trackRecordRepository;
    private readonly ITrackGenreRepository _trackGenreRepository;
    private readonly IVoteRepository _voteRepository;

    public PlaylistApiFunction(
        ILogger<PlaylistApiFunction> logger,
        IGroupChatRepository groupChatRepository,
        ITrackRecordRepository trackRecordRepository,
        ITrackGenreRepository trackGenreRepository,
        IVoteRepository voteRepository)
    {
        _logger = logger;
        _groupChatRepository = groupChatRepository;
        _trackRecordRepository = trackRecordRepository;
        _trackGenreRepository = trackGenreRepository;
        _voteRepository = voteRepository;
    }

    /// <summary>
    /// Gets all playlists.
    /// </summary>
    [Function("GetPlaylists")]
    public async Task<IActionResult> GetPlaylists(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/playlists")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all playlists.");

            var playlists = await _groupChatRepository.GetAllWithPlaylistsAsync(cancellationToken);

            var result = PlaylistMapper.ToDto(playlists);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playlists.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets a specific playlist by chat ID.
    /// </summary>
    [Function("GetPlaylist")]
    public async Task<IActionResult> GetPlaylist(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/playlists/{chatId:long}")] HttpRequest req,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting playlist for chat ID: {ChatId}", chatId);

            var playlist = await _groupChatRepository.GetByTelegramChatIdAsync(chatId, cancellationToken);

            if (playlist == null)
            {
                return new NotFoundResult();
            }

            var result = PlaylistMapper.ToDto(playlist);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting playlist for chat ID: {ChatId}", chatId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets tracks for a playlist with pagination.
    /// </summary>
    [Function("GetPlaylistTracks")]
    public async Task<IActionResult> GetPlaylistTracks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/playlists/{chatId:long}/tracks")] HttpRequest req,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting tracks for chat ID: {ChatId}", chatId);

            // Get pagination parameters.
            var skipParam = req.Query["skip"].FirstOrDefault();
            var takeParam = req.Query["take"].FirstOrDefault();

            int skip = int.TryParse(skipParam, out var s) ? s : 0;
            int take = int.TryParse(takeParam, out var t) ? t : 10000;

            var tracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, skip, take, cancellationToken);

            // Filter out deleted tracks.
            var nonDeletedTracks = tracks.Where(t => !t.IsDeleted).ToList();

            // Get track IDs for genre lookup.
            var trackSpotifyIds = nonDeletedTracks.Select(t => t.TrackSpotifyId).Distinct().ToList();

            // Load genres for all tracks.
            var trackGenresMap = new Dictionary<string, List<string>>();
            foreach (var trackId in trackSpotifyIds)
            {
                var genres = await _trackGenreRepository.GetGenresForTrackAsync(trackId, cancellationToken);
                trackGenresMap[trackId] = genres.ToList();
            }

            // Load votes for all tracks.
            var votesMap = new Dictionary<string, List<VoteDto>>();
            foreach (var track in nonDeletedTracks)
            {
                var votes = await _voteRepository.GetByTrackRecordAsync(track.TrackRecordId, cancellationToken);
                votesMap[track.TrackRecordId] = VoteMapper.ToDto(votes);
            }

            var result = nonDeletedTracks.Select(t => TrackMapper.ToDto(
                t,
                trackGenresMap.ContainsKey(t.TrackSpotifyId) ? trackGenresMap[t.TrackSpotifyId] : new List<string>(),
                votesMap.ContainsKey(t.TrackRecordId) ? votesMap[t.TrackRecordId] : new List<VoteDto>()
            ));

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tracks for chat ID: {ChatId}", chatId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets contributors for a playlist.
    /// </summary>
    [Function("GetPlaylistContributors")]
    public async Task<IActionResult> GetPlaylistContributors(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/playlists/{chatId:long}/contributors")] HttpRequest req,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting contributors for chat ID: {ChatId}", chatId);

            var contributors = await _trackRecordRepository.GetContributorsAsync(chatId, cancellationToken);

            var result = ContributorMapper.ToDto(contributors);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contributors for chat ID: {ChatId}", chatId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets genres for a playlist.
    /// </summary>
    [Function("GetPlaylistGenres")]
    public async Task<IActionResult> GetPlaylistGenres(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/playlists/{chatId:long}/genres")] HttpRequest req,
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting genres for chat ID: {ChatId}", chatId);

            // Get all tracks for the playlist.
            var tracks = await _trackRecordRepository.GetByGroupChatAsync(chatId, 0, 10000, cancellationToken);
            var nonDeletedTracks = tracks.Where(t => !t.IsDeleted).ToList();
            var trackSpotifyIds = nonDeletedTracks.Select(t => t.TrackSpotifyId).Distinct().ToList();

            // Get genres for all tracks.
            var genres = await _trackGenreRepository.GetGenresForTracksAsync(trackSpotifyIds, cancellationToken);

            var result = GenreMapper.ToDto(genres);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting genres for chat ID: {ChatId}", chatId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
