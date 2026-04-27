import { initStatWidget } from './widget-stat';
import { initTimelineWidget } from './widget-timeline';
import { initTopCategoriesWidget } from './widget-top-categories';
import { initTopIdeasWidget } from './widget-top-ideas';
import { initSurveyDistributionWidget } from './widget-survey-distribution';
import { initIdeaFeedWidget } from './widget-idea-feed';
import { initModerationQueueWidget } from './widget-moderation-queue';

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
