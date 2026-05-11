// Wires the project "Stijl" section: colour picker + "use theme default"
// checkbox both feed a single hidden input (PrimaryColor) so an empty value
// reaches the server when the user wants the theme's own colour.

const picker     = document.getElementById('primary-color-picker') as HTMLInputElement | null;
const hidden     = document.getElementById('PrimaryColor')         as HTMLInputElement | null;
const useDefault = document.getElementById('use-theme-color')      as HTMLInputElement | null;

if (picker && hidden && useDefault) {
    const initial = hidden.value.trim();
    if (initial === '') {
        useDefault.checked = true;
        picker.disabled = true;
    } else {
        useDefault.checked = false;
        picker.disabled = false;
        picker.value = initial;
    }

    useDefault.addEventListener('change', () => {
        if (useDefault.checked) {
            picker.disabled = true;
            hidden.value = '';
        } else {
            picker.disabled = false;
            hidden.value = picker.value;
        }
    });

    picker.addEventListener('input', () => {
        if (!useDefault.checked) hidden.value = picker.value;
    });
}
