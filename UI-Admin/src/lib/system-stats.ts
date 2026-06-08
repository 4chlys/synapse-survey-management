import { fetchJson } from '../admin/dashboard';

export interface ServiceCostRow {
    service: string;
    model: string;
    inputTokens: number;
    outputTokens: number;
    requestCount: number;
    costEur: number;
}

export interface ProjectStatsRow {
    projectId: number;
    projectName: string;
    shortForm: string;
    platformId: number;
    platformName: string;
    state: string;
    participantCount: number;
    startedCount: number;
    avgCompletionSeconds: number;
    medianCompletionSeconds: number;
    minCompletionSeconds: number;
    q1CompletionSeconds: number;
    q3CompletionSeconds: number;
    maxCompletionSeconds: number;
}

export interface SystemStats {
    platformCount: number;
    activeProjectCount: number;
    draftProjectCount: number;
    archivedProjectCount: number;
    totalParticipants: number;
    avgCompletionSeconds: number;
    medianCompletionSeconds: number;
    projects: ProjectStatsRow[];
    aiUsageBySystem: ServiceCostRow[];
}

export function fetchSystemStats(platformId: number | null, aiFrom?: string): Promise<SystemStats> {
    const params = new URLSearchParams();
    if (platformId !== null) params.set('platformId', String(platformId));
    if (aiFrom) params.set('aiFrom', aiFrom);
    const qs = params.toString();
    return fetchJson<SystemStats>(qs ? `/api/system-stats?${qs}` : '/api/system-stats');
}
