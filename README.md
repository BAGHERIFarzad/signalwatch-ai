# SignalWatch AI

SignalWatch AI is a live web intelligence agent built for the Web Data UNLOCKED Hackathon.

It helps enterprise teams collect, analyze, and act on live public web signals about companies, competitors, pricing, hiring, product launches, and risk indicators.

The project uses Bright Data SERP API to collect live public web data and Gemini AI to transform those signals into executive-ready intelligence reports.

---

## Problem

Enterprise teams often rely on stale reports, manual competitor research, or fragmented public data sources.

Sales, finance, strategy, and compliance teams need a faster way to monitor live public web signals and turn them into business decisions.

SignalWatch AI solves this by combining live web data collection with AI-powered analysis.

---

## Solution

SignalWatch AI allows a user to enter:

- Company name
- Company website
- Competitors
- Hackathon track
- Monitoring goal

The system then:

1. Builds targeted search queries.
2. Collects live public web signals using Bright Data SERP API.
3. Filters low-value search results.
4. Classifies signals into GTM, Finance / Market, or Security / Compliance.
5. Uses Gemini AI to generate:
   - Executive summary
   - Risk score
   - Opportunity score
   - Recommended actions
6. Displays the final report in a React dashboard.

---

## Hackathon Track

Primary track:

- GTM Intelligence

Secondary applicable tracks:

- Finance & Market Intelligence
- Security & Compliance

---

## Bright Data Integration

This project uses:

- Bright Data SERP API

Bright Data is used to collect live public web search data from Google results.  
The integration is implemented in the .NET backend through `BrightDataService.cs`.

---

## AI Integration

This project uses:

- Gemini API

Gemini analyzes the live signals collected through Bright Data and generates a structured intelligence report.

If Gemini is unavailable, the backend falls back to rule-based scoring and recommendations.

---

## Tech Stack

### Backend

- ASP.NET Core Web API
- C#
- HttpClient
- Bright Data SERP API
- Gemini API
- Swagger

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS
- Axios
- Lucide React

---

## Project Structure

```txt
SignalWatchAI/
├── backend/
│   └── SignalWatch.Api/
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
├── frontend/
│   └── signalwatch-ui/
│       ├── src/
│       │   ├── pages/
│       │   ├── services/
│       │   └── types/
├── docs/
│   ├── architecture.md
│   ├── demo-script.md
│   ├── submission.md
│   └── screenshots/
├── .gitignore
└── README.md