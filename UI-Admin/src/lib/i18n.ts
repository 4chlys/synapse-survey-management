type Strings = {
    submit: string;
    cancel: string;
    save: string;
    delete: string;
    edit: string;
    add: string;
    back: string;
    search: string;
    yes: string;
    no: string;
    confirm: string;
    loading: string;
    close: string;
    all: string;
    none: string;
};

declare global {
    interface Window {
        __i18n: { culture: string; strings: Strings };
    }
}

export const t = <K extends keyof Strings>(key: K): string => window.__i18n.strings[key];
export const currentCulture = (): string => window.__i18n.culture;
