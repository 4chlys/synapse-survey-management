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

const el = document.getElementById('i18n-data');
const data: { culture: string; strings: Strings } = el
    ? JSON.parse(el.textContent ?? '{}')
    : { culture: '', strings: {} as Strings };

export const t = <K extends keyof Strings>(key: K): string => data.strings[key];
export const currentCulture = (): string => data.culture;
