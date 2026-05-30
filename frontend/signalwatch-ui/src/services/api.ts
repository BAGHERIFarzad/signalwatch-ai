import axios from "axios";
import type {
  IntelligenceRequest,
  IntelligenceReport,
} from "../types/intelligence";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5093/api",
  headers: {
    "Content-Type": "application/json",
  },
});

export async function generateDemoReport(
  request: IntelligenceRequest
): Promise<IntelligenceReport> {
  const response = await api.post<IntelligenceReport>(
    "/Intelligence/demo",
    request
  );

  return response.data;
}

export async function generateLiveReport(
  request: IntelligenceRequest
): Promise<IntelligenceReport> {
  const response = await api.post<IntelligenceReport>(
    "/Intelligence/live",
    request
  );

  return response.data;
}