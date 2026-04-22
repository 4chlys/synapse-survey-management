import { UserConfig, defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';

export default defineConfig(async () => {
    const files = fs.readdirSync('./src');
    const inputEntries = files
        .filter(file => file.endsWith('.ts'))
        .reduce((acc, file) => {
            const fileName = path.parse(file).name;
            acc[fileName] = path.join('./src', file);
            return acc;
        }, {} as Record<string, string>);

    const config: UserConfig = {
        plugins: [tailwindcss()],
        appType: 'custom',
        base: '/dist/',
        root: 'src',
        publicDir: 'public',
        build: {
            emptyOutDir: false,
            manifest: true,
            outDir: '../wwwroot/dist',
            assetsDir: '',
            rollupOptions: {
                input: inputEntries
            },
        },
        server: {
            strictPort: true,
        },
        css: {
            preprocessorOptions: {
                scss: {
                    api: "modern-compiler",
                },
            },
        },
    }

    return config;
});
