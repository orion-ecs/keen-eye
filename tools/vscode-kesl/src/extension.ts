import * as path from 'path';
import * as vscode from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient | undefined;

export function activate(context: vscode.ExtensionContext) {
    const config = vscode.workspace.getConfiguration('kesl.lsp');
    const enabled = config.get<boolean>('enabled', true);

    if (!enabled) {
        console.log('KESL language server is disabled');
        return;
    }

    const serverPath = findServerPath(config.get<string>('path', ''));
    if (!serverPath) {
        vscode.window.showWarningMessage(
            'KESL language server not found. Install the KeenEyes SDK or set kesl.lsp.path.'
        );
        return;
    }

    const serverOptions: ServerOptions = {
        run: {
            command: serverPath,
            transport: TransportKind.stdio
        },
        debug: {
            command: serverPath,
            transport: TransportKind.stdio
        }
    };

    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'kesl' }],
        synchronize: {
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.kesl')
        }
    };

    client = new LanguageClient(
        'keslLanguageServer',
        'KESL Language Server',
        serverOptions,
        clientOptions
    );

    client.start();
    console.log('KESL language server started');
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }
    return client.stop();
}

function findServerPath(configuredPath: string): string | undefined {
    // Use configured path if provided
    if (configuredPath && configuredPath.trim()) {
        return configuredPath;
    }

    // Try to find the server in common locations
    const possibleNames = [
        'KeenEyes.Lsp.Kesl',
        'KeenEyes.Lsp.Kesl.exe',
        'keeneyes-lsp-kesl',
        'keeneyes-lsp-kesl.exe'
    ];

    // Check if any are in PATH
    const pathDirs = (process.env.PATH || '').split(path.delimiter);
    for (const dir of pathDirs) {
        for (const name of possibleNames) {
            const fullPath = path.join(dir, name);
            try {
                const fs = require('fs');
                if (fs.existsSync(fullPath)) {
                    return fullPath;
                }
            } catch {
                // Ignore errors
            }
        }
    }

    // Try relative to workspace
    const workspaceFolders = vscode.workspace.workspaceFolders;
    if (workspaceFolders) {
        for (const folder of workspaceFolders) {
            // Common locations for local builds
            const localPaths = [
                path.join(folder.uri.fsPath, '.lsp', 'KeenEyes.Lsp.Kesl.exe'),
                path.join(folder.uri.fsPath, '.lsp', 'KeenEyes.Lsp.Kesl'),
                path.join(folder.uri.fsPath, 'tools', 'KeenEyes.Lsp.Kesl', 'bin', 'Debug', 'net10.0', 'KeenEyes.Lsp.Kesl.exe'),
                path.join(folder.uri.fsPath, 'tools', 'KeenEyes.Lsp.Kesl', 'bin', 'Release', 'net10.0', 'KeenEyes.Lsp.Kesl.exe')
            ];

            for (const localPath of localPaths) {
                try {
                    const fs = require('fs');
                    if (fs.existsSync(localPath)) {
                        return localPath;
                    }
                } catch {
                    // Ignore errors
                }
            }
        }
    }

    return undefined;
}
