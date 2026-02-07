# Requirements Document

## Introduction

This document specifies the requirements for a Telegram bot application that automatically detects Spotify track URLs in group chat messages and adds them to a configured Spotify playlist. The bot enables group members to collaboratively build playlists by simply sharing Spotify links in their conversations.

## Glossary

- **Bot**: The Telegram bot application that processes messages and interacts with Spotify
- **Administrator**: The user who adds the Bot to a group chat and has configuration privileges
- **Group_Chat**: A Telegram group conversation where the Bot is installed
- **Spotify_Track_URL**: A URL that references a specific track on Spotify (e.g., https://open.spotify.com/track/...)
- **Target_Playlist**: The Spotify playlist configured by the Administrator for a specific Group_Chat
- **Webhook**: An HTTP endpoint that receives message events from Telegram
- **Spotify_Credentials**: OAuth tokens that authorize the Bot to modify playlists on behalf of the Administrator, or allow users to add tracks to their queue.
- **Database**: The persistent storage system for all bot configuration and track history
- **Track_Record**: A database entry containing track information, the user who shared it, and voting data
- **Web_Frontend**: A Blazor web application for browsing playlists and track history
- **Upvote**: A positive vote on a track by a group member
- **Downvote**: A negative vote on a track by a group member
- **Chat_History_Sync**: The process of scanning past messages to add previously shared tracks
- **Spotify_Queue**: The current playback queue in a user's Spotify account
- **Auto_Queue**: A user preference to automatically add playlist tracks to their Spotify_Queue
- **Downvote_Threshold**: The configurable number of downvotes required to remove a track from the playlist

## Requirements

### Requirement 1: Webhook Message Reception

**User Story:** As a system, I want to receive message events from Telegram via webhooks, so that I can process messages in real-time without polling.

#### Acceptance Criteria

1. WHEN a message is posted in a Group_Chat where the Bot is installed, THE Bot SHALL receive the message content via webhook
2. WHEN the webhook receives a message event, THE Bot SHALL extract the message text and chat identifier
3. IF the webhook receives an invalid or malformed request, THEN THE Bot SHALL log the error and return an appropriate HTTP status code
4. THE Bot SHALL acknowledge webhook requests within 5 seconds to prevent Telegram timeouts

### Requirement 2: Spotify URL Detection

**User Story:** As a group member, I want the bot to automatically detect when I share Spotify track links, so that tracks can be added without manual commands.

#### Acceptance Criteria

1. WHEN a message contains a Spotify_Track_URL, THE Bot SHALL identify and extract the track identifier
2. THE Bot SHALL recognize Spotify track URLs in the format "https://open.spotify.com/track/{track_id}"
3. THE Bot SHALL recognize Spotify track URLs with query parameters (e.g., "?si=...")
4. WHEN a message contains multiple Spotify_Track_URLs, THE Bot SHALL detect and process each URL independently
5. WHEN a message contains no Spotify_Track_URL, THE Bot SHALL ignore the message and take no action

### Requirement 3: Administrator Assignment

**User Story:** As a user who adds the bot to a group, I want to automatically become the administrator, so that I can configure the bot without additional setup steps.

#### Acceptance Criteria

1. WHEN the Bot is added to a Group_Chat, THE Bot SHALL record the user who added it as the Administrator
2. THE Bot SHALL store the Administrator identifier associated with the Group_Chat identifier
3. WHEN the Bot is added to a Group_Chat, THE Bot SHALL send a welcome message explaining administrator privileges
4. IF the Bot cannot determine who added it, THEN THE Bot SHALL send an error message to the Group_Chat

### Requirement 4: Spotify Authentication

**User Story:** As a user, I want to authenticate with my Spotify account, so that I can use personalized features like auto-queueing tracks.

#### Acceptance Criteria

1. WHEN any user requests authentication, THE Bot SHALL initiate the Spotify OAuth flow in a private chat with that user
2. THE Bot SHALL not send authentication links or credentials in the Group_Chat
3. THE Bot SHALL request the "playlist-modify-public", "playlist-modify-private", and "user-modify-playback-state" Spotify scopes
4. WHEN the OAuth flow completes successfully, THE Bot SHALL store the Spotify_Credentials in the Database associated with the user
5. WHEN Spotify_Credentials expire, THE Bot SHALL use the refresh token to obtain new credentials automatically
6. IF the OAuth flow fails or is cancelled, THEN THE Bot SHALL notify the user in the private chat and maintain the unauthenticated state
7. THE Bot SHALL allow both Administrators and regular group members to authenticate

### Requirement 5: Playlist Configuration

**User Story:** As an administrator, I want to configure which Spotify playlist my group chat is connected to, so that tracks are added to the correct playlist.

#### Acceptance Criteria

1. WHEN an Administrator configures a playlist, THE Bot SHALL validate that the playlist exists and is accessible
2. THE Bot SHALL store the Target_Playlist identifier associated with the Group_Chat identifier in the Database
3. WHEN an Administrator changes the Target_Playlist, THE Bot SHALL update the configuration and confirm the change
4. THE Bot SHALL allow each Group_Chat to have a different Target_Playlist configuration
5. IF an Administrator attempts to configure a playlist they don't have access to, THEN THE Bot SHALL reject the configuration and explain the error

### Requirement 18: Downvote Threshold Configuration

**User Story:** As an administrator, I want to configure how many downvotes are needed to remove a track, so that I can adjust the moderation sensitivity for my group.

#### Acceptance Criteria

1. THE Bot SHALL set the default Downvote_Threshold to 3 for new Group_Chats
2. WHEN an Administrator changes the Downvote_Threshold, THE Bot SHALL store the new value in the Database associated with the Group_Chat
3. THE Bot SHALL validate that the Downvote_Threshold is a positive integer
4. WHEN a non-Administrator attempts to change the Downvote_Threshold, THE Bot SHALL reject the command
5. THE Bot SHALL apply the current Downvote_Threshold when evaluating whether to remove tracks

### Requirement 19: Auto-Queue Configuration

**User Story:** As a user, I want tracks added to the playlist to automatically be added to my Spotify queue, so that I can discover new music without manual effort.

#### Acceptance Criteria

1. WHEN an authenticated user enables Auto_Queue for a Group_Chat, THE Bot SHALL store this preference in the Database
2. THE Bot SHALL allow users to configure Auto_Queue independently for each Group_Chat they are in
3. WHEN a track is added to a Target_Playlist and a user has Auto_Queue enabled for that Group_Chat, THE Bot SHALL add the track to that user's Spotify_Queue
4. IF adding to the Spotify_Queue fails because the user is not playing music, THE Bot SHALL silently skip without notifying the user
5. WHEN a user disables Auto_Queue for a Group_Chat, THE Bot SHALL update the preference in the Database
6. THE Bot SHALL require users to authenticate with Spotify before enabling Auto_Queue

### Requirement 20: Manual Queue Addition

**User Story:** As a user, I want to manually add tracks to my Spotify queue from the group chat, so that I can listen to interesting tracks immediately.

#### Acceptance Criteria

1. WHEN the Bot confirms a track was added to the playlist, THE Bot SHALL include a "Add to Queue" button in the confirmation message
2. WHEN a user clicks the "Add to Queue" button, THE Bot SHALL add the track to that user's Spotify_Queue
3. IF the user has not authenticated with Spotify, THE Bot SHALL prompt them to authenticate in a private chat
4. WHEN the track is successfully added to the queue, THE Bot SHALL confirm the action to the user
5. THE Bot SHALL allow any user to add tracks to their own queue regardless of who shared the track

### Requirement 6: Track Addition to Playlist

**User Story:** As a group member, I want detected Spotify tracks to be automatically added to our group's playlist, so that we can build a collaborative playlist effortlessly.

#### Acceptance Criteria

1. WHEN a Spotify_Track_URL is detected in a Group_Chat with a configured Target_Playlist, THE Bot SHALL add the track to the Target_Playlist using the Administrator's Spotify_Credentials
2. WHEN a track is successfully added, THE Bot SHALL reply to the original message with a confirmation and a link to the Target_Playlist
3. IF a track is already in the Target_Playlist, THEN THE Bot SHALL not add it again and SHALL reply to the original message indicating the track was already present
4. IF the track addition fails due to authentication issues, THEN THE Bot SHALL notify the Administrator in a private chat to re-authenticate
5. IF the track addition fails for other reasons, THEN THE Bot SHALL log the error and reply to the original message with a user-friendly error message

### Requirement 11: Track History Recording

**User Story:** As a system, I want to record all track sharing activity in the database, so that I can provide detailed history and analytics.

#### Acceptance Criteria

1. WHEN a Spotify_Track_URL is detected and processed, THE Bot SHALL create a Track_Record in the Database
2. THE Track_Record SHALL include the Spotify track identifier, the user who shared it, the Group_Chat identifier, and the timestamp
3. WHEN a user attempts to add a track that is already in the Target_Playlist, THE Bot SHALL still create a Track_Record marking it as a duplicate attempt
4. THE Bot SHALL retrieve track metadata from the Spotify API including track name, artist, album, and genre information
5. THE Bot SHALL store track, artist, album, and genre information in normalized tables to avoid duplication across playlists
6. THE Track_Record SHALL reference the normalized track metadata rather than duplicating it
7. THE Bot SHALL store all Track_Records permanently for historical analysis

### Requirement 12: Track Voting

**User Story:** As a group member, I want to upvote or downvote tracks in the playlist, so that the group can collectively curate the playlist quality.

#### Acceptance Criteria

1. WHERE Telegram supports emoji reactions on messages, THE Bot SHALL allow users to vote using thumbs up (👍) or thumbs down (👎) reactions on the confirmation message
2. WHERE Telegram does not support emoji reactions, THE Bot SHALL include upvote and downvote buttons in the confirmation message
3. WHEN a user adds an upvote reaction, THE Bot SHALL record an Upvote in the Database associated with the Track_Record and the user
4. WHEN a user adds a downvote reaction, THE Bot SHALL record a Downvote in the Database associated with the Track_Record and the user
5. THE Bot SHALL allow each user to vote only once per track (either upvote or downvote)
6. WHEN a user changes their vote from upvote to downvote or vice versa, THE Bot SHALL update their vote in the Database
7. WHEN a user removes their vote reaction, THE Bot SHALL delete their vote from the Database
8. THE Bot SHALL update the confirmation message to display the current upvote and downvote counts
9. WHEN a track is removed due to downvotes, THE Bot SHALL prevent any further voting on that track

### Requirement 13: Automatic Track Removal

**User Story:** As a group member, I want tracks with too many downvotes to be automatically removed, so that unpopular tracks don't clutter our playlist.

#### Acceptance Criteria

1. WHEN a track's total downvote count reaches the Downvote_Threshold, THE Bot SHALL remove the track from the Target_Playlist
2. THE Downvote_Threshold SHALL default to 3 downvotes
3. THE Bot SHALL count absolute downvotes without subtracting upvotes
4. WHEN a track is removed due to downvotes, THE Bot SHALL send a message to the Group_Chat indicating the track was removed
5. THE Bot SHALL mark the Track_Record as deleted in the Database but SHALL not delete the record
6. WHEN a deleted track's Spotify_Track_URL is shared again, THE Bot SHALL not add it to the playlist and SHALL notify that it was previously removed

### Requirement 14: Chat History Synchronization

**User Story:** As an administrator, I want to sync historical messages to add tracks that were shared before the bot was added, so that I don't lose the group's previous music sharing history.

#### Acceptance Criteria

1. WHEN an Administrator triggers a sync operation, THE Bot SHALL process historical messages to detect Spotify_Track_URLs
2. WHERE the Telegram API allows chat history access, THE Bot SHALL retrieve and scan historical messages from the Group_Chat
3. WHERE the Telegram API does not allow chat history access, THE Bot SHALL accept an exported JSON file containing the chat history from the Administrator
4. WHEN processing historical messages, THE Bot SHALL create Track_Records with the original sharing user and timestamp
5. THE Bot SHALL add detected tracks to the Target_Playlist respecting the chronological order
6. THE Bot SHALL ignore tracks that are already in the Target_Playlist or were previously deleted due to downvotes
7. THE Bot SHALL not send individual confirmation messages for each historical track to avoid spam
8. WHEN the sync completes, THE Bot SHALL send a single summary message indicating how many tracks were added
9. IF the sync operation fails, THEN THE Bot SHALL notify the Administrator with error details

### Requirement 15: Web Frontend - Playlist Browsing

**User Story:** As a user, I want to browse playlists through a web interface, so that I can explore track history and voting data outside of Telegram.

#### Acceptance Criteria

1. THE Web_Frontend SHALL display a list of all available playlists
2. WHEN a user selects a playlist, THE Web_Frontend SHALL display all tracks in that playlist
3. THE Web_Frontend SHALL display track metadata including name, artist, album, and genre
4. THE Web_Frontend SHALL display who shared each track and when it was shared
5. THE Web_Frontend SHALL display the current upvote and downvote counts for each track

### Requirement 16: Web Frontend - User Filtering

**User Story:** As a user, I want to see which tracks each person contributed, so that I can explore music recommendations from specific group members.

#### Acceptance Criteria

1. WHEN viewing a playlist, THE Web_Frontend SHALL display a list of all users who contributed tracks
2. THE Web_Frontend SHALL display the number of tracks each user contributed
3. WHEN a user clicks on a contributor, THE Web_Frontend SHALL filter the track list to show only tracks shared by that user
4. THE Web_Frontend SHALL allow clearing the user filter to return to the full track list

### Requirement 17: Web Frontend - Genre Filtering

**User Story:** As a user, I want to filter tracks by genre, so that I can explore specific types of music in the playlist.

#### Acceptance Criteria

1. WHEN viewing a playlist, THE Web_Frontend SHALL display a list of all genres present in the playlist
2. THE Web_Frontend SHALL display the number of tracks in each genre
3. WHEN a user clicks on a genre, THE Web_Frontend SHALL filter the track list to show only tracks of that genre
4. THE Web_Frontend SHALL allow clearing the genre filter to return to the full track list
5. WHERE a track has multiple genres, THE Web_Frontend SHALL include it in all applicable genre filters

### Requirement 7: Configuration State Management

**User Story:** As an administrator, I want the bot to guide me through setup, so that I understand what configuration is needed before tracks can be added.

#### Acceptance Criteria

1. WHEN a Spotify_Track_URL is detected in a Group_Chat without Spotify authentication, THE Bot SHALL notify the Administrator to authenticate with Spotify
2. WHEN a Spotify_Track_URL is detected in a Group_Chat without a configured Target_Playlist, THE Bot SHALL notify the Administrator to configure a playlist
3. THE Bot SHALL not attempt to add tracks until both authentication and playlist configuration are complete
4. WHEN an Administrator completes authentication, THE Bot SHALL check if playlist configuration is still needed and prompt accordingly
5. WHEN an Administrator completes playlist configuration, THE Bot SHALL check if authentication is still needed and prompt accordingly

### Requirement 8: Administrator-Only Commands

**User Story:** As an administrator, I want configuration commands to be restricted to me, so that other group members cannot change the bot's settings.

#### Acceptance Criteria

1. WHEN a non-Administrator user attempts to execute a configuration command, THE Bot SHALL reject the command and explain that only the Administrator can configure the bot
2. THE Bot SHALL verify the user's identity against the stored Administrator identifier before processing configuration commands
3. WHEN an Administrator executes a configuration command, THE Bot SHALL process it normally
4. THE Bot SHALL allow all group members to share Spotify_Track_URLs regardless of administrator status

### Requirement 9: Data Persistence

**User Story:** As a system, I want all configuration and track data stored in a database, so that the application remains stateless and can scale horizontally.

#### Acceptance Criteria

1. THE Bot SHALL store Administrator assignments for each Group_Chat in the Database
2. THE Bot SHALL store Spotify_Credentials for each Administrator in the Database
3. THE Bot SHALL store Target_Playlist configurations for each Group_Chat in the Database
4. WHEN a webhook is received, THE Bot SHALL retrieve all necessary configuration data from the Database
5. THE Bot SHALL encrypt sensitive data such as Spotify_Credentials before storing in the Database
6. THE Bot SHALL not maintain in-memory state between webhook requests

### Requirement 10: Error Handling and Logging

**User Story:** As a system administrator, I want comprehensive error logging, so that I can diagnose and fix issues when they occur.

#### Acceptance Criteria

1. WHEN an error occurs during message processing, THE Bot SHALL log the error with sufficient context for debugging
2. WHEN an error occurs during Spotify API calls, THE Bot SHALL log the error including the API response
3. WHEN an error occurs during webhook processing, THE Bot SHALL log the error and return an appropriate HTTP status code
4. THE Bot SHALL log all configuration changes made by Administrators
5. THE Bot SHALL not log sensitive information such as access tokens in plain text
