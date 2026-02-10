# Implementation Plan: Telegram Spotify Bot

## Overview

This implementation plan breaks down the Telegram Spotify Bot into discrete coding tasks. The approach follows an incremental development strategy where each task builds on previous work, with early validation through testing. The bot will be built using .NET 10, Azure Functions, and Azure Table Storage.

## Tasks

- [x] 1. Set up project structure and core infrastructure
  - Create .NET 10 solution with projects for Azure Functions, Blazor frontend, and shared libraries
  - Configure Azure Table Storage connection and repository pattern
  - Set up Sentry integration for error logging
  - Create base entity classes for Table Storage (ITableEntity implementations)
  - Configure dependency injection for all services
  - _Requirements: 9.4, 9.5, 10.1_

- [x] 2. Implement Azure Table Storage repositories
  - [x] 2.1 Create repository interfaces and base repository class
    - Define IRepository<T> interface with common CRUD operations
    - Implement BaseRepository<T> with Table Storage client
    - Add error handling and retry logic for transient failures
    - _Requirements: 9.4_
  
  - [x] 2.2 Implement GroupChat repository
    - Create GroupChatRepository with methods for CRUD operations
    - Implement IsPlaylistLinkedAsync to enforce unique playlist constraint
    - _Requirements: 3.1, 3.2, 5.2, 5.4_
  
  - [x] 2.3 Implement User repository
    - Create UserRepository with methods for CRUD operations
    - Implement credential encryption/decryption
    - _Requirements: 4.4, 9.5_
  
  - [x] 2.4 Implement TrackRecord repository
    - Create TrackRecordRepository with pagination support
    - Implement methods to query by group chat and check deleted tracks
    - _Requirements: 11.1, 11.2, 13.5_
  
  - [x] 2.5 Implement Vote repository
    - Create VoteRepository with upsert and delete operations
    - Implement vote counting methods
    - _Requirements: 12.3, 12.4, 12.6, 12.7_
  
  - [x] 2.6 Write property test for configuration round-trip
    - **Property 5: Configuration Round-Trip**
    - **Validates: Requirements 3.2, 5.2, 9.4, 18.2**

- [x] 3. Checkpoint - Verify repository layer
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement Spotify URL detection
  - [x] 4.1 Create SpotifyUrlDetector service
    - Implement regex pattern: `(https?://(open|play)\.spotify\.com/track/|spotify:track:)([\w\d]+)`
    - Create DetectTrackUrls method to find all URLs in text
    - Create ExtractTrackId method to extract track ID from URL
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [ ]* 4.2 Write property test for URL detection
    - **Property 3: Spotify URL Detection**
    - **Validates: Requirements 2.1, 2.3, 2.4**
  
  - [ ]* 4.3 Write property test for no false positives
    - **Property 4: No False Positives in URL Detection**
    - **Validates: Requirements 2.5**

- [-] 5. Implement Spotify API integration
  - [x] 5.1 Create SpotifyService with authentication
    - Integrate SpotifyAPI.Web NuGet package
    - Implement GetTrackAsync to retrieve track metadata
    - Implement RefreshAccessTokenAsync for token refresh
    - _Requirements: 4.5, 11.4_
  
  - [x] 5.2 Implement playlist operations
    - Implement AddTrackToPlaylistAsync
    - Implement RemoveTrackFromPlaylistAsync
    - Implement playlist validation
    - _Requirements: 5.1, 6.1, 13.1_
  
  - [x] 5.3 Implement queue operations
    - Implement AddTrackToQueueAsync
    - Implement IsUserPlayingAsync to check playback state
    - _Requirements: 19.3, 19.4, 20.2_
  
  - [x] 5.4 Write property test for credential refresh
    - **Property 7: Credential Persistence and Refresh**
    - **Validates: Requirements 4.5**

- [x] 6. Implement OAuth authentication flow (Azure Function)
  - [x] 6.1 Create SpotifyOAuthFunction
    - Implement StartAuth endpoint to initiate OAuth flow
    - Generate and store OAuth state parameter
    - Redirect to Spotify authorization URL with correct scopes
    - _Requirements: 4.1, 4.3_
  
  - [x] 6.2 Implement OAuth callback handler
    - Implement HandleCallback endpoint
    - Validate state parameter
    - Exchange authorization code for tokens
    - Store encrypted credentials with scope in Table Storage
    - Send confirmation via Telegram private message
    - _Requirements: 4.4, 4.6_
  
  - [ ]* 6.3 Write property test for OAuth private chat enforcement
    - **Property 6: OAuth Private Chat Enforcement**
    - **Validates: Requirements 4.1, 4.2**
  
  - [ ]* 6.4 Write property test for authentication access control
    - **Property 8: Authentication Access Control**
    - **Validates: Requirements 4.7, 8.4**

- [x] 7. Checkpoint - Verify authentication and Spotify integration
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Implement Telegram webhook handler (Azure Function)
  - [ ] 8.1 Create TelegramWebhookFunction
    - Implement HTTP trigger for webhook endpoint
    - Validate webhook signature from Telegram
    - Parse Telegram Update object
    - Route to appropriate handlers based on update type
    - Log errors to Sentry
    - _Requirements: 1.2, 1.3, 10.3_
  
  - [ ]* 8.2 Write property test for webhook payload parsing
    - **Property 1: Webhook Payload Parsing**
    - **Validates: Requirements 1.2**
  
  - [ ]* 8.3 Write property test for invalid webhook handling
    - **Property 2: Invalid Webhook Handling**
    - **Validates: Requirements 1.3, 10.3**

- [ ] 9. Implement message handler core logic
  - [ ] 9.1 Create MessageHandler service
    - Implement HandleMessageAsync for text messages
    - Ensure user exists in Table Storage (create if needed)
    - Detect Spotify URLs in message text
    - Retrieve group configuration from Table Storage
    - _Requirements: 1.2, 2.1, 9.4_
  
  - [ ] 9.2 Implement bot added to group handler
    - Implement HandleBotAddedToGroupAsync
    - Create group chat record in Table Storage
    - Set administrator to user who added bot
    - Send welcome message explaining administrator privileges
    - _Requirements: 3.1, 3.2, 3.3_
  
  - [ ] 9.3 Implement configuration state validation
    - Check if group has Spotify authentication
    - Check if group has configured playlist
    - Send appropriate prompts if configuration incomplete
    - Prevent track addition until fully configured
    - _Requirements: 7.1, 7.2, 7.3_

- [ ] 10. Implement track addition workflow
  - [ ] 10.1 Create track metadata fetching and storage
    - Fetch track metadata from Spotify API
    - Store normalized track, artist, album, genre data in Table Storage
    - Handle denormalization for TrackRecord entities
    - _Requirements: 11.4, 11.5, 20.1_
  
  - [ ] 10.2 Implement track addition logic
    - Add track to Spotify playlist using administrator credentials
    - Create TrackRecord in Table Storage
    - Handle duplicate detection
    - Send confirmation reply with playlist link
    - _Requirements: 6.1, 6.2, 6.3, 11.1, 11.2, 11.3_
  
  - [ ]* 10.3 Write property test for track addition with credentials
    - **Property 11: Track Addition with Credentials**
    - **Validates: Requirements 6.1**
  
  - [ ]* 10.4 Write property test for duplicate detection
    - **Property 12: Duplicate Track Detection**
    - **Validates: Requirements 6.3, 11.3**
  
  - [ ]* 10.5 Write property test for metadata normalization
    - **Property 19: Track Metadata Normalization**
    - **Validates: Requirements 11.5**

- [ ] 11. Implement voting system
  - [ ] 11.1 Create VoteManager service
    - Implement RecordVoteAsync to handle upvotes and downvotes
    - Implement RemoveVoteAsync for vote removal
    - Update denormalized vote counts in TrackRecord
    - Check if track should be removed based on threshold
    - _Requirements: 12.3, 12.4, 12.6, 12.7, 12.8_
  
  - [ ] 11.2 Implement reaction handler
    - Implement HandleMessageReactionAsync
    - Detect thumbs up/down reactions
    - Call VoteManager to record votes
    - Update confirmation message with vote counts
    - _Requirements: 12.1, 12.8_
  
  - [ ] 11.3 Implement automatic track removal
    - Check downvote count against threshold
    - Remove track from Spotify playlist when threshold reached
    - Mark TrackRecord as deleted
    - Send notification to group chat
    - Prevent further voting on deleted tracks
    - _Requirements: 13.1, 13.3, 13.4, 13.5, 12.9_
  
  - [ ]* 11.4 Write property test for vote recording
    - **Property 21: Vote Recording and Updates**
    - **Validates: Requirements 12.3, 12.4, 12.6**
  
  - [ ]* 11.5 Write property test for one vote per user
    - **Property 22: One Vote Per User Per Track**
    - **Validates: Requirements 12.5**
  
  - [ ]* 11.6 Write property test for automatic removal
    - **Property 26: Automatic Track Removal at Threshold**
    - **Validates: Requirements 13.1, 13.3, 13.5**
  
  - [ ]* 11.7 Write property test for deleted track re-addition prevention
    - **Property 27: Deleted Track Re-Addition Prevention**
    - **Validates: Requirements 13.6**

- [ ] 12. Checkpoint - Verify core bot functionality
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Implement bot commands
  - [ ] 13.1 Implement /auth command
    - Send OAuth link in private chat
    - Verify user authentication status
    - _Requirements: 4.1_
  
  - [ ] 13.2 Implement /configure command
    - Allow administrator to set playlist ID
    - Validate playlist exists and is accessible
    - Store playlist name in GroupChat entity
    - Enforce unique playlist constraint
    - _Requirements: 5.1, 5.2, 5.3_
  
  - [ ] 13.3 Implement /threshold command
    - Allow administrator to set downvote threshold
    - Validate threshold is positive integer
    - Store in GroupChat entity
    - _Requirements: 18.2, 18.3, 18.5_
  
  - [ ] 13.4 Implement /autoqueue command
    - Allow users to enable/disable auto-queue per group
    - Require Spotify authentication
    - Store preference in UserGroupConfig
    - _Requirements: 19.1, 19.6, 19.7_
  
  - [ ]* 13.5 Write property test for administrator-only commands
    - **Property 14: Administrator-Only Commands**
    - **Validates: Requirements 8.1, 8.3**
  
  - [ ]* 13.6 Write property test for threshold validation
    - **Property 28: Downvote Threshold Validation**
    - **Validates: Requirements 18.3**

- [ ] 14. Implement auto-queue functionality
  - [ ] 14.1 Implement auto-queue trigger
    - Check UserGroupConfig for users with auto-queue enabled
    - Verify user is currently playing music
    - Add track to user's Spotify queue
    - Handle failures silently
    - _Requirements: 19.3, 19.4, 19.5_
  
  - [ ]* 14.2 Write property test for auto-queue conditional execution
    - **Property 41: Auto-Queue Conditional Execution**
    - **Validates: Requirements 19.3, 19.4**

- [ ] 15. Implement manual queue addition
  - [ ] 15.1 Add "Add to Queue" button to confirmation messages
    - Include button in track confirmation reply
    - _Requirements: 20.1_
  
  - [ ] 15.2 Implement callback handler for queue button
    - Implement HandleCallbackQueryAsync
    - Check user authentication
    - Check if user is playing music
    - Add track to user's queue
    - Send confirmation or error message
    - _Requirements: 20.2, 20.3, 20.4, 20.5_
  
  - [ ]* 15.3 Write property test for manual queue addition
    - **Property 43: Manual Queue Addition**
    - **Validates: Requirements 20.2, 20.6**

- [ ] 16. Implement chat history sync
  - [ ] 16.1 Create ChatHistorySyncService
    - Implement SyncFromTelegramHistoryAsync (if API allows)
    - Implement SyncFromExportedJsonAsync for JSON file upload
    - Process messages chronologically
    - Detect Spotify URLs in historical messages
    - _Requirements: 14.1, 14.2, 14.3_
  
  - [ ] 16.2 Implement sync track processing
    - Create TrackRecords with original timestamps and users
    - Skip duplicates and deleted tracks
    - Add tracks to playlist in chronological order
    - Don't send individual confirmation messages
    - Send summary message at end
    - _Requirements: 14.4, 14.5, 14.6, 14.7, 14.8_
  
  - [ ] 16.3 Implement /sync command
    - Allow administrator to trigger sync
    - Handle both API-based and file-based sync
    - Log errors to Sentry
    - _Requirements: 14.1, 14.9_
  
  - [ ]* 16.4 Write property test for sync track detection
    - **Property 30: Chat History Sync Track Detection**
    - **Validates: Requirements 14.1, 14.4**
  
  - [ ]* 16.5 Write property test for sync filtering
    - **Property 32: Sync Filtering**
    - **Validates: Requirements 14.6**

- [ ] 17. Checkpoint - Verify all bot features
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 18. Implement Telegram ID change detection
  - [ ] 18.1 Add ID change detection logic
    - Compare current user ID with stored ID
    - Log changes to Sentry with context
    - Update user records if needed
    - _Requirements: 10.1_

- [ ] 19. Implement user merge service
  - [ ] 19.1 Create UserMergeService
    - Implement MergeUsersAsync method
    - Update all TrackRecords to reference new user ID
    - Update all Votes to reference new user ID
    - Update all UserGroupConfigs to reference new user ID
    - Update all GroupChats where user is administrator
    - Update all GroupChatMembers to reference new user ID
    - Delete old user entity
    - Log merge operation to Sentry
    - _Requirements: 10.1_
  
  - [ ] 19.2 Create admin interface for user merging
    - Add admin page in Blazor frontend
    - Allow selecting two users to merge
    - Confirm merge operation
    - Display merge history

- [ ] 20. Implement Blazor frontend - Playlist pages
  - [ ] 20.1 Create PlaylistList page
    - Display all playlists with track counts
    - Link to playlist detail pages
    - _Requirements: 15.1_
  
  - [ ] 20.2 Create PlaylistDetail page
    - Display tracks with pagination (handle thousands of tracks)
    - Show track metadata (name, artist, album, genre)
    - Show who shared each track and when
    - Display vote counts
    - Show contributor list
    - Show genre list
    - Implement filtering by contributor and genre
    - _Requirements: 15.2, 15.3, 15.4, 15.5_
  
  - [ ] 20.3 Create TrackCard component
    - Display track information
    - Show user avatars for sharer and voters
    - Display list of users who upvoted/downvoted
    - _Requirements: 15.3, 15.4, 15.5_
  
  - [ ]* 20.4 Write property test for track metadata display
    - **Property 34: Web Frontend Track Metadata Display**
    - **Validates: Requirements 15.3, 15.4, 15.5**

- [ ] 21. Implement Blazor frontend - User pages
  - [ ] 21.1 Create UserList page
    - Display all users with contribution statistics
    - Show total upvotes/downvotes given and received
    - Link to user detail pages
    - _Requirements: 16.1_
  
  - [ ] 21.2 Create UserDetail page
    - Display user's Telegram avatar
    - Show total upvotes/downvotes given (overall and per playlist)
    - Show total upvotes/downvotes received on shared tracks (overall and per playlist)
    - Show list of users who upvoted/downvoted their tracks
    - Show tracks shared by user ordered by upvotes descending
    - Show per-playlist statistics
    - _Requirements: 16.2, 16.3_
  
  - [ ]* 21.3 Write property test for user filter accuracy
    - **Property 35: User Filter Accuracy**
    - **Validates: Requirements 16.3**
  
  - [ ]* 21.4 Write property test for contributor count accuracy
    - **Property 36: Contributor Count Accuracy**
    - **Validates: Requirements 16.2**

- [ ] 22. Implement Blazor frontend - Genre filtering
  - [ ] 22.1 Add genre filter functionality
    - Display genre list with track counts
    - Implement genre filtering
    - Handle multi-genre tracks
    - Allow clearing filters
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5_
  
  - [ ]* 22.2 Write property test for genre filter accuracy
    - **Property 37: Genre Filter Accuracy**
    - **Validates: Requirements 17.3**
  
  - [ ]* 22.3 Write property test for multi-genre inclusion
    - **Property 38: Multi-Genre Track Inclusion**
    - **Validates: Requirements 17.5**

- [ ] 23. Implement error handling and logging
  - [ ] 23.1 Add comprehensive error logging
    - Log all errors with context to Sentry
    - Implement structured logging
    - Add correlation IDs for request tracing
    - Redact sensitive data from logs
    - _Requirements: 10.1, 10.2, 10.4, 10.5_
  
  - [ ]* 23.2 Write property test for error logging
    - **Property 16: Error Logging with Context**
    - **Validates: Requirements 10.1, 10.2, 10.4**
  
  - [ ]* 23.3 Write property test for sensitive data redaction
    - **Property 17: Sensitive Data Redaction in Logs**
    - **Validates: Requirements 10.5**

- [ ] 24. Set up GitHub Actions CI/CD pipeline
  - [ ] 24.1 Create workflow for build and test
    - Set up .NET 10 build
    - Run all unit and property tests
    - Generate test coverage report
  
  - [ ] 24.2 Create workflow for Azure Functions deployment
    - Build and publish Azure Functions
    - Deploy to Azure using publish profile
    - Configure environment variables from Key Vault
  
  - [ ] 24.3 Create workflow for Blazor frontend deployment
    - Build and publish Blazor app
    - Deploy to Azure App Service or Static Web Apps
    - Configure connection strings

- [ ] 25. Final checkpoint - End-to-end testing
  - Ensure all tests pass, ask the user if questions arise.
  - Verify webhook integration with Telegram
  - Test OAuth flow with real Spotify account
  - Verify track addition and voting in test group
  - Test frontend displays data correctly
  - Verify Sentry receives error logs

## Notes

- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using xUnit Theory with InlineData
- Unit tests validate specific examples and edge cases
- The implementation uses .NET 10, Azure Functions, Azure Table Storage, and Blazor
- All sensitive data (tokens, credentials) must be encrypted before storage
- Sentry integration provides error tracking and monitoring
- GitHub Actions automates deployment to Azure
