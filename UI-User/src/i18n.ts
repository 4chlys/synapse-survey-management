type Strings = { submit: string; cancel: string; back: string; next: string };

declare global {
    interface Window {
        __i18n: { culture: string; strings: Strings };
    }
}

export const t = <K extends keyof Strings>(key: K): string => window.__i18n.strings[key];
export const currentCulture = (): string => window.__i18n.culture;
