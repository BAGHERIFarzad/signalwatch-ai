export interface IntelligenceRequest {
  companyName: string;
  companyWebsite: string;
  competitors: string[];
  track: string;
  monitoringGoal: string;
}

export interface WebSignal {
  title: string;
  source: string;
  url: string;
  signalType: string;
  summary: string;
  confidenceScore: number;
}

export interface IntelligenceReport {
  companyName: string;
  track: string;
  executiveSummary: string;
  riskScore: number;
  opportunityScore: number;
  signals: WebSignal[];
  recommendedActions: string[];
  generatedAt: string;
}