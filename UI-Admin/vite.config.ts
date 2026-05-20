import { UserConfig, defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';

export default defineConfig(async () => {
    // Recursively collect entry .ts files under ./src, skipping ./src/lib
    // (lib contains shared modules imported by entries, not entries themselves).
    const inputEntries: Record<string, string> = {};
    const walk = (dir: string): void => {
        for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
            const full = path.join(dir, entry.name);
            if (entry.isDirectory()) {
                if (entry.name === 'lib') continue;
                walk(full);
            } else if (entry.isFile() && entry.name.endsWith('.ts')) {
                const key = path.parse(entry.name).name;
                inputEntries[key] = full;
            }
        }
    };
    walk('./src');

    const config: UserConfig = {
        plugins: [tailwindcss()],
        appType: 'custom',
        base: '/dist/',
        root: 'src',
        publicDir: 'public',
        build: {
            emptyOutDir: true,
            manifest: true,
            outDir: '../wwwroot/dist',
            assetsDir: '',
            rollupOptions: {
                input: inputEntries
            },
        },
        server: {
            port: 5174,
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