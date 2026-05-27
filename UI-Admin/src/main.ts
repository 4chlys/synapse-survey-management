import './main.css';
import { initTheme } from './theme';
import { initDomHandlers } from './lib/dom-handlers';
import { initAlerts } from './lib/alerts';
import { initPasswordToggle } from './lib/password-toggle';
import { initExportEmails } from './lib/export-emails';
import { initSettingsPrompts } from './settings/settings-prompts';
import './admin/admin-ai-usage';
import './project/project-index';
import './project/project-setup';
import './project/project-style';
import './project/project-shortform-autofill';
import './project/project-questions';
import './project/question-detail-modal';
import './questionnaire/questionnaire-dnd';
import './project/project-ideation';
import './admin/ideas-filter';
import './admin/ideas-status-badge';
import './admin/rawdata-export';
import './admin/system-stats-export';
import './project/project-stats-export';

function init(): void {
    initTheme();
    initDomHandlers();
    initAlerts();
    initPasswordToggle();
    initExportEmails();
    initSettingsPrompts();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}
