# Product Overview

KiroSpotiBot is a Telegram bot that automatically detects Spotify track URLs shared in group chats and adds them to collaborative playlists. The bot enables group members to build playlists together through voting, with automatic track removal based on downvotes.

## Core Features

- Automatic Spotify URL detection in Telegram group chats
- Collaborative playlist building with voting system (upvotes/downvotes)
- OAuth authentication for Spotify integration
- Auto-queue tracks to user's Spotify queue
- Web frontend (Blazor) for browsing playlist history, user activity, and genre filtering
- Chat history synchronization

## Architecture Philosophy

- Stateless, webhook-driven serverless architecture
- Ultra-low-cost NoSQL storage (Azure Table Storage)
- Event-driven processing via Azure Functions
- Property-based testing for correctness guarantees
