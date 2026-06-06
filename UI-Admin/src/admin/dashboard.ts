import { initStatWidget } from '../widgets/widget-stat';
import { initTimelineWidget } from '../widgets/widget-timeline';
import { initTopCategoriesWidget } from '../widgets/widget-top-categories';
import { initTopIdeasWidget } from '../widgets/widget-top-ideas';
import { initSurveyDistributionWidget } from '../lib/widget-survey-distribution';
import { initIdeaFeedWidget } from '../widgets/widget-idea-feed';
import { initModerationQueueWidget } from '../widgets/widget-moderation-queue';
import { initSystemParticipantsWidget } from '../widgets/widget-system-participants';
import { initSystemProjectStatesWidget } from '../widgets/widget-system-project-states';
import { initSystemAiCostWidget } from '../widgets/widget-system-ai-cost';
import { initSystemCompletionBoxplotWidget } from '../widgets/widget-system-completion-boxplot';
import { initQuestionSinglePieWidget } from '../widgets/widget-question-single-pie';
import { initQuestionMultiBarWidget } from '../widgets/widget-question-multi-bar';
import { initQuestionLikertBarWidget } from '../widgets/widget-question-likert-bar';
import { initSocialMediaWidget } from '../lib/widget-social-media';
import type { WidgetInit } from '../lib/admin-types';

export const POLL_INTERVAL_MS = 15_000;

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
    QuestionSinglePie: initQuestionSinglePieWidget,
    QuestionMultiBar: initQuestionMultiBarWidget,
    QuestionLikertBar: initQuestionLikertBarWidget,
    SocialMediaPost: initSocialMediaWidget,
};

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

export function initDashboard(): void {
    const slots = document.querySelectorAll<HTMLElement>('[data-widget]');
    if (!slots.length) return;

    const cleanups: Array<() => void> = [];

    document.querySelectorAll<HTMLElement>('.db-grid > *').forEach((el, i) => {
        el.style.animationDelay = `${i * 0.04}s`;
    });

    slots.forEach(el => {
        if (el.dataset.widgetInitialized === 'true') return;
        el.dataset.widgetInitialized = 'true';

        const type = el.dataset.widget ?? '';
        const init = widgetRegistry[type];
        if (!init) return;

        const cleanup = init(el);
        if (cleanup) cleanups.push(cleanup);
    });

    window.addEventListener('beforeunload', () => cleanups.forEach(fn => fn()));
}
