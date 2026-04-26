/**
 * RVM.HealthGuard — Gerador de Manual HTML
 *
 * Le os screenshots gerados pelo Playwright e produz um manual HTML standalone.
 *
 * Uso:
 *   cd docs && npx tsx generate-html.ts
 *
 * Saida:
 *   docs/manual-usuario.html
 *   docs/manual-usuario.md
 */
import fs from 'fs';
import path from 'path';

const SCREENSHOTS_DIR = path.resolve(__dirname, 'screenshots');
const OUTPUT_HTML = path.resolve(__dirname, 'manual-usuario.html');
const OUTPUT_MD = path.resolve(__dirname, 'manual-usuario.md');

interface Section {
  id: string;
  title: string;
  description: string;
  screenshot: string;
  features: string[];
  tips?: string[];
}

const sections: Section[] = [
  {
    id: 'dashboard',
    title: '1. Dashboard de Monitoramento',
    description:
      'Visao geral em tempo real do estado de todos os servicos monitorados. ' +
      'Exibe status atual, tempo de resposta e disponibilidade geral do sistema.',
    screenshot: '01-dashboard',
    features: [
      'Status global: ONLINE / DEGRADADO / OFFLINE',
      'Tempo de resposta medio de todos os servicos',
      'Uptime percentual geral (ultimos 30 dias)',
      'Contagem de incidentes abertos',
      'Atualizacao automatica via SignalR (sem reload de pagina)',
      'Historico de status em grafico de barras (ultimas 24h)',
    ],
  },
  {
    id: 'services',
    title: '2. Servicos Monitorados',
    description:
      'Lista completa de endpoints HTTP monitorados. Para cada servico, exibe ' +
      'status atual, tempo de resposta, frequencia de verificacao e uptime historico.',
    screenshot: '02-services',
    features: [
      'Listagem de todos os endpoints monitorados',
      'Status individual: UP / DOWN / DEGRADED',
      'Tempo de resposta atual e media dos ultimos 7 dias',
      'Uptime percentual por servico',
      'Intervalo de verificacao configuravel (30s a 24h)',
      'Adicionar, editar e remover servicos',
      'Tags para agrupamento por ambiente ou time',
    ],
    tips: [
      'Use intervalos de 1 minuto para servicos criticos e 5 minutos para os demais.',
      'Tags como "producao" e "staging" facilitam o filtro no dashboard.',
    ],
  },
  {
    id: 'incidents',
    title: '3. Incidentes',
    description:
      'Registro cronologico de todos os incidentes detectados. ' +
      'Um incidente e criado automaticamente quando um servico fica indisponivel ' +
      'e fechado quando ele volta ao normal.',
    screenshot: '03-incidents',
    features: [
      'Lista de incidentes abertos e fechados',
      'Duracao, servico afetado e causa raiz',
      'Timeline de eventos dentro do incidente',
      'Notificacoes enviadas durante o incidente',
      'Comentarios e notas de pos-mortem',
      'Exportacao de relatorio por periodo',
    ],
    tips: [
      'Use as notas de pos-mortem para registrar causa raiz e acoes corretivas.',
    ],
  },
  {
    id: 'uptime',
    title: '4. Relatorio de Uptime',
    description:
      'Historico detalhado de disponibilidade por servico. ' +
      'Permite visualizar uptime por periodo: 7, 30, 60 e 90 dias.',
    screenshot: '04-uptime',
    features: [
      'Uptime percentual por servico e por periodo',
      'Grafico de disponibilidade diaria (barras coloridas)',
      'Tempo total de downtime no periodo',
      'SLA calculado automaticamente',
      'Exportacao CSV para relatorios',
    ],
  },
  {
    id: 'alerts',
    title: '5. Configuracao de Alertas',
    description:
      'Gerenciamento de canais de notificacao e regras de alerta. ' +
      'Suporta notificacoes via e-mail, webhook e Slack quando um servico muda de estado.',
    screenshot: '05-alerts',
    features: [
      'Canais suportados: E-mail, Webhook HTTP, Slack',
      'Regras por servico ou por grupo de servicos',
      'Condicoes: DOWN, DEGRADED, RECOVERED',
      'Cooldown de alertas para evitar spam',
      'Teste de canal antes de salvar',
      'Historico de notificacoes enviadas',
    ],
    tips: [
      'Configure o cooldown para pelo menos 5 minutos em servicos com oscilacoes frequentes.',
      'Use o botao "Testar" para confirmar que o canal esta funcionando antes de ativar.',
    ],
  },
];

// ---------------------------------------------------------------------------
// Utilitarios
// ---------------------------------------------------------------------------
function imageToBase64(filePath: string): string | null {
  if (!fs.existsSync(filePath)) return null;
  const buffer = fs.readFileSync(filePath);
  return `data:image/png;base64,${buffer.toString('base64')}`;
}

function generateHTML(): string {
  const now = new Date().toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  let sectionsHtml = '';
  for (const s of sections) {
    const desktopPath = path.join(SCREENSHOTS_DIR, `${s.screenshot}--desktop.png`);
    const mobilePath = path.join(SCREENSHOTS_DIR, `${s.screenshot}--mobile.png`);
    const desktopImg = imageToBase64(desktopPath);
    const mobileImg = imageToBase64(mobilePath);

    const featuresHtml = s.features.map((f) => `<li>${f}</li>`).join('\n            ');
    const tipsHtml = s.tips
      ? `<div class="tips">
          <strong>Dicas:</strong>
          <ul>${s.tips.map((t) => `<li>${t}</li>`).join('\n            ')}</ul>
        </div>`
      : '';

    const screenshotsHtml = desktopImg
      ? `<div class="screenshots">
          <div class="screenshot-group">
            <span class="badge">Desktop</span>
            <img src="${desktopImg}" alt="${s.title} - Desktop" />
          </div>
          ${
            mobileImg
              ? `<div class="screenshot-group mobile">
              <span class="badge">Mobile</span>
              <img src="${mobileImg}" alt="${s.title} - Mobile" />
            </div>`
              : ''
          }
        </div>`
      : '<p class="no-screenshot"><em>Screenshot nao disponivel. Execute o script Playwright para gerar.</em></p>';

    sectionsHtml += `
    <section id="${s.id}">
      <h2>${s.title}</h2>
      <p class="description">${s.description}</p>
      <div class="features">
        <strong>Funcionalidades:</strong>
        <ul>
            ${featuresHtml}
        </ul>
      </div>
      ${tipsHtml}
      ${screenshotsHtml}
    </section>`;
  }

  return `<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>RVM.HealthGuard - Manual do Usuario</title>
  <style>
    :root { --primary: #16a34a; --surface: #ffffff; --bg: #f4f6fa; --text: #1e293b; --text-muted: #64748b; --border: #e2e8f0; --sidebar-bg: #0f172a; --accent: #22c55e; }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: var(--bg); color: var(--text); line-height: 1.6; }
    .container { max-width: 1100px; margin: 0 auto; padding: 2rem 1.5rem; }
    header { background: var(--sidebar-bg); color: white; padding: 3rem 1.5rem; text-align: center; }
    header h1 { font-size: 2rem; margin-bottom: 0.5rem; }
    header p { color: #94a3b8; font-size: 1rem; }
    header .version { color: #64748b; font-size: 0.85rem; margin-top: 0.5rem; }
    nav { background: var(--surface); border-bottom: 1px solid var(--border); padding: 1rem 1.5rem; position: sticky; top: 0; z-index: 100; }
    nav .container { padding: 0; }
    nav ul { list-style: none; display: flex; flex-wrap: wrap; gap: 0.5rem; }
    nav a { display: inline-block; padding: 0.35rem 0.75rem; border-radius: 0.5rem; font-size: 0.85rem; color: var(--text); text-decoration: none; background: var(--bg); transition: background 0.2s; }
    nav a:hover { background: var(--primary); color: white; }
    section { background: var(--surface); border: 1px solid var(--border); border-radius: 1rem; padding: 2rem; margin-bottom: 2rem; }
    section h2 { font-size: 1.5rem; color: var(--primary); margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 2px solid var(--border); }
    .description { font-size: 1.05rem; margin-bottom: 1.25rem; color: var(--text); }
    .features, .tips { background: var(--bg); border-radius: 0.75rem; padding: 1rem 1.25rem; margin-bottom: 1.25rem; }
    .features ul, .tips ul { margin-top: 0.5rem; padding-left: 1.25rem; }
    .features li, .tips li { margin-bottom: 0.35rem; }
    .tips { background: #f0fdf4; border-left: 4px solid var(--accent); }
    .tips strong { color: var(--primary); }
    .screenshots { display: flex; gap: 1.5rem; margin-top: 1rem; align-items: flex-start; }
    .screenshot-group { position: relative; flex: 1; border: 1px solid var(--border); border-radius: 0.75rem; overflow: hidden; }
    .screenshot-group.mobile { flex: 0 0 200px; max-width: 200px; }
    .screenshot-group img { width: 100%; display: block; }
    .badge { position: absolute; top: 0.5rem; right: 0.5rem; background: var(--sidebar-bg); color: white; font-size: 0.7rem; padding: 0.2rem 0.5rem; border-radius: 0.35rem; font-weight: 600; text-transform: uppercase; }
    .no-screenshot { background: var(--bg); padding: 2rem; border-radius: 0.75rem; text-align: center; color: var(--text-muted); }
    footer { text-align: center; padding: 2rem 1rem; color: var(--text-muted); font-size: 0.85rem; }
    @media (max-width: 768px) { .screenshots { flex-direction: column; } .screenshot-group.mobile { max-width: 100%; flex: 1; } section { padding: 1.25rem; } }
    @media print { nav { display: none; } section { break-inside: avoid; page-break-inside: avoid; } .screenshots { flex-direction: column; } .screenshot-group.mobile { max-width: 250px; } }
  </style>
</head>
<body>
  <header>
    <h1>RVM.HealthGuard - Manual do Usuario</h1>
    <p>Monitoramento HTTP em Tempo Real — Guia Completo de Funcionalidades</p>
    <div class="version">Gerado em ${now} | RVM Tech</div>
  </header>

  <nav>
    <div class="container">
      <ul>
        ${sections.map((s) => `<li><a href="#${s.id}">${s.title}</a></li>`).join('\n        ')}
      </ul>
    </div>
  </nav>

  <div class="container">
    <section id="visao-geral">
      <h2>Visao Geral</h2>
      <p class="description">
        O <strong>RVM.HealthGuard</strong> e um sistema de monitoramento HTTP em tempo real.
        Verifica periodicamente a disponibilidade de endpoints, detecta incidentes automaticamente,
        notifica a equipe e gera relatorios de uptime e SLA.
      </p>
      <div class="features">
        <strong>Recursos principais:</strong>
        <ul>
          <li><strong>Monitoramento HTTP</strong> — verificacao periodica de endpoints (30s a 24h)</li>
          <li><strong>Alertas em tempo real</strong> — notificacoes via e-mail, webhook e Slack</li>
          <li><strong>Incidentes automaticos</strong> — criados e fechados sem intervencao manual</li>
          <li><strong>Uptime e SLA</strong> — relatorios para periodos de 7, 30, 60 e 90 dias</li>
          <li><strong>Dashboard SignalR</strong> — atualizacoes em tempo real sem reload</li>
          <li><strong>Status page publica</strong> — pagina de status compartilhavel</li>
        </ul>
      </div>
    </section>

    ${sectionsHtml}
  </div>

  <footer>
    <p>RVM Tech &mdash; Monitoramento HTTP</p>
    <p>Documento gerado automaticamente com Playwright + TypeScript</p>
  </footer>
</body>
</html>`;
}

function generateMarkdown(): string {
  const now = new Date().toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

  let md = `# RVM.HealthGuard - Manual do Usuario

> Monitoramento HTTP em Tempo Real — Guia Completo de Funcionalidades
>
> Gerado em ${now} | RVM Tech

---

## Visao Geral

O **RVM.HealthGuard** monitora endpoints HTTP em tempo real, detecta incidentes e notifica a equipe.

**Recursos principais:**
- **Monitoramento HTTP** — verificacao periodica de endpoints (30s a 24h)
- **Alertas em tempo real** — notificacoes via e-mail, webhook e Slack
- **Incidentes automaticos** — criados e fechados sem intervencao manual
- **Uptime e SLA** — relatorios para periodos de 7, 30, 60 e 90 dias
- **Dashboard SignalR** — atualizacoes em tempo real sem reload

---

`;

  for (const s of sections) {
    const desktopExists = fs.existsSync(path.join(SCREENSHOTS_DIR, `${s.screenshot}--desktop.png`));

    md += `## ${s.title}\n\n`;
    md += `${s.description}\n\n`;
    md += `**Funcionalidades:**\n`;
    for (const f of s.features) md += `- ${f}\n`;
    md += '\n';

    if (s.tips) {
      md += `> **Dicas:**\n`;
      for (const t of s.tips) md += `> - ${t}\n`;
      md += '\n';
    }

    if (desktopExists) {
      md += `| Desktop | Mobile |\n|---------|--------|\n`;
      md += `| ![${s.title} - Desktop](screenshots/${s.screenshot}--desktop.png) | ![${s.title} - Mobile](screenshots/${s.screenshot}--mobile.png) |\n`;
    } else {
      md += `*Screenshot nao disponivel. Execute o script Playwright para gerar.*\n`;
    }
    md += '\n---\n\n';
  }

  md += `## Informacoes Tecnicas

| Item | Detalhe |
|------|---------|
| **Tecnologia** | ASP.NET Core + Blazor Server |
| **Tempo real** | SignalR (WebSocket) |
| **Banco de dados** | PostgreSQL 16 |
| **Notificacoes** | E-mail, Webhook HTTP, Slack |

---

*Documento gerado automaticamente com Playwright + TypeScript — RVM Tech*
`;

  return md;
}

const html = generateHTML();
fs.writeFileSync(OUTPUT_HTML, html, 'utf-8');
console.log(`HTML gerado: ${OUTPUT_HTML}`);

const md = generateMarkdown();
fs.writeFileSync(OUTPUT_MD, md, 'utf-8');
console.log(`Markdown gerado: ${OUTPUT_MD}`);
