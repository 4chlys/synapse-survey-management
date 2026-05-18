import { initStatWidget } from './widget-stat';
import { initTimelineWidget } from './widget-timeline';
import { initTopCategoriesWidget } from './widget-top-categories';
import { initTopIdeasWidget } from './widget-top-ideas';
import { initSurveyDistributionWidget } from './lib/widget-survey-distribution';
import { initIdeaFeedWidget } from './widget-idea-feed';
import { initModerationQueueWidget } from './widget-moderation-queue';
import { initSystemParticipantsWidget } from './widget-system-participants';
import { initSystemProjectStatesWidget } from './widget-system-project-states';
import { initSystemAiCostWidget } from './widget-system-ai-cost';
import { initSystemCompletionBoxplotWidget } from './widget-system-completion-boxplot';

const POLL_INTERVAL_MS = 30_000;

type WidgetInit = (el: HTMLElement) => (() => void) | void;

const widgetRegistry: Record<string, WidgetInit> = {
    ViewCount: initStatWidget,
    CompletionCount: initStatWidget,
    CompletionRate: initStatWidget,
    ParticipationTimeline: initTimelineWidget,
    TopCategories: initTopCategoriesWidget,
    TopIdeas: initTopIdeasWidget,
    SurveyDistribution: initSurveyDistributionWidget,
    IdeaFeed: initIdeaFeedWidget,
    ModerationQueue: initModerationQueueWidget,
    SystemParticipants: initSystemParticipantsWidget,
    SystemProjectStates: initSystemProjectStatesWidget,
    SystemAiCost: initSystemAiCostWidget,
    SystemCompletionBoxplot: initSystemCompletionBoxplotWidget,
};

document.addEventListener('DOMContentLoaded', () => {
    const slots = document.querySelectorAll<HTMLElement>('[data-widget]');
    const cleanups: Array<() => void> = [];

    slots.forEach(el => {
        const type = el.dataset.widget ?? '';
        const init = widgetRegistry[type];
        if (!init) return;

        const cleanup = init(el);
        if (cleanup) cleanups.push(cleanup);
    });

    window.addEventListener('beforeunload', () => cleanups.forEach(fn => fn()));
});

export { POLL_INTERVAL_MS };

const inflightRequests = new Map<string, Promise<unknown>>();

export function fetchJson<T>(url: string): Promise<T> {
    if (!inflightRequests.has(url)) {
        const p = fetch(url)
            .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json() as Promise<T>; })
            .finally(() => inflightRequests.delete(url));
        inflightRequests.set(url, p);
    }
    return inflightRequests.get(url) as Promise<T>;
}
