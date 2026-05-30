# SignalWatch AI Architecture

## Overview

SignalWatch AI is a full-stack live web intelligence agent built for the Web Data UNLOCKED Hackathon.

The application collects live public web signals through Bright Data SERP API, filters and classifies the results, then uses Gemini AI to produce an executive intelligence report.

The report includes:

- Executive summary
- Risk score
- Opportunity score
- Detected signals
- Source links
- Recommended actions

---

## Architecture Flow

```txt
User
  ↓
React + TypeScript Dashboard
  ↓
ASP.NET Core Web API
  ↓
SignalOrchestratorService
  ↓
BrightDataService
  ↓
Bright Data SERP API
  ↓
Live public web search results
  ↓
Filtering + signal classification
  ↓
AiAnalysisService
  ↓
Gemini API
  ↓
IntelligenceReport
  ↓
React Dashboard
```

---

## Backend Components

### IntelligenceController

Exposes the main API endpoints:

```txt
GET  /api/Intelligence/health
POST /api/Intelligence/demo
POST /api/Intelligence/live
```

### BrightDataService

Responsible for:

- Building targeted search queries
- Calling Bright Data SERP API
- Parsing Bright Data JSON responses
- Filtering low-value Google menu/navigation results
- Returning clean search results

### SignalOrchestratorService

Responsible for:

- Orchestrating the full intelligence workflow
- Converting Bright Data results into business-ready WebSignal objects
- Calculating fallback scores
- Calling Gemini AI analysis
- Returning the final IntelligenceReport

### AiAnalysisService

Responsible for:

- Sending live signals to Gemini
- Requesting JSON-formatted analysis
- Parsing the AI response
- Returning executive summary, risk score, opportunity score, and recommended actions

---

## Frontend Components

### Dashboard.tsx

Main user interface for:

- Company input
- Competitor input
- Track selection
- Demo report generation
- Live Bright Data report generation
- Report display
- Source links
- Markdown report export
- Copy executive summary

### api.ts

Axios client used to call the .NET backend.

---

## Data Models

### IntelligenceRequest

Represents the user input.

### BrightDataSearchResult

Represents parsed search results from Bright Data.

### WebSignal

Represents a business-ready intelligence signal.

### IntelligenceReport

Final report returned to the frontend.

---

## Resilience

If Gemini fails or the API key is unavailable, the backend falls back to rule-based analysis.

This keeps the demo stable even if the AI provider has rate limits or temporary errors.

---

## Security

Secrets are loaded from environment variables.

API keys are never stored in the frontend and should never be committed to GitHub.
