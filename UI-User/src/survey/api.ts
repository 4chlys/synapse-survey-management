import { getCsrfToken } from '../lib/utils';

function getHeaders(): HeadersInit {
    return {
        'Content-Type': 'application/json',
        'RequestVerificationToken': getCsrfToken(),
    };
}

export async function startSession(projectId: number): Promise<string> {
    const res = await fetch(`/${projectId}/api/SurveyApi/participants`, {
        method: 'POST',
        headers: getHeaders(),
    });
    if (!res.ok) throw new Error('Failed to start session');
    const data = await res.json() as { participantToken: string };
    return data.participantToken;
}

export async function saveResponse(projectId: number, questionId: number, value: string): Promise<void> {
    const res = await fetch(`/${projectId}/api/SurveyApi/responses`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ questionId, value }),
    });
    if (!res.ok) throw new Error('Failed to save response');
}

export async function getNextQuestion(projectId: number, currentQuestionId: number): Promise<number | null> {
    const res = await fetch(
        `/${projectId}/api/SurveyApi/next-question?currentQuestionId=${currentQuestionId}`
    );
    if (!res.ok) throw new Error('Failed to get next question');
    const data = await res.json() as { questionId: number | null };
    return data.questionId;
}

export async function getPreviousQuestion(projectId: number, currentQuestionId: number): Promise<number | null> {
    const res = await fetch(
        `/${projectId}/api/SurveyApi/previous-question?currentQuestionId=${currentQuestionId}`
    );
    if (!res.ok) throw new Error('Failed to get previous question');
    const data = await res.json() as { questionId: number | null };
    return data.questionId;
}

export async function getVisiblePath(projectId: number): Promise<number[]> {
    const res = await fetch(`/${projectId}/api/SurveyApi/visible-path`);
    if (!res.ok) throw new Error('Failed to get visible path');
    const data = await res.json() as { questionIds: number[] };
    return data.questionIds;
}

export async function getResumeQuestion(projectId: number): Promise<number | null> {
    const res = await fetch(`/${projectId}/api/SurveyApi/resume-question`);
    if (!res.ok) throw new Error('Failed to get resume question');
    const data = await res.json() as { questionId: number | null };
    return data.questionId;
}

export async function submitSurvey(projectId: number): Promise<void> {
    const res = await fetch(`/${projectId}/api/SurveyApi/submit`, {
        method: 'POST',
        headers: getHeaders(),
    });
    if (!res.ok) throw new Error('Failed to submit survey');
}

export async function removeAllResponses(projectId: number): Promise<void> {
    const res = await fetch(`/${projectId}/api/SurveyApi/responses`, {
        method: 'DELETE',
        headers: getHeaders(),
    });
    if (!res.ok) throw new Error('Failed to remove responses');
}
