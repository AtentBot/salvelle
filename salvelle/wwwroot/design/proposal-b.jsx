/* global React, Section, Swatch, TypeRow, TokenCell */

function ClinicalProposal() {
  return (
    <div className="proposal clinical">
      <header className="proposal-header">
        <div>
          <div className="brand-mark">
            <div className="brand-glyph">o</div>
            <div className="brand-name">Salvelle</div>
          </div>
          <h1 className="proposal-title">Proposta B — Clinical</h1>
          <p className="proposal-tagline">
            Identidade clínico-moderna. Sans humanista, paleta azul-petróleo
            sóbria sobre neutros frios. Sensação de software médico contemporâneo,
            sem decoração desnecessária.
          </p>
        </div>
        <div className="proposal-meta">
          <div>Direction B · 02</div>
          <div>Petrol / Cool</div>
          <div>General Sans</div>
        </div>
      </header>

      <Section eyebrow="01 / Paleta" title="Cor" sub="Primária única — petróleo profundo. Neutros frios para superfícies. Status com soft-fill, sem aplicação como decoração.">
        <div className="grid-4" style={{ marginBottom: 24 }}>
          <Swatch bg="var(--teal-500)" name="Petrol 500 — Primária" hex="#14706B" tone="dark" />
          <Swatch bg="var(--teal-700)" name="Petrol 700" hex="#0A4845" tone="dark" />
          <Swatch bg="var(--ink)" name="Ink" hex="#0B1220" tone="dark" />
          <Swatch bg="var(--ink-3)" name="Ink 3" hex="#5C6878" tone="dark" />
          <Swatch bg="var(--surface)" name="Surface" hex="#FFFFFF" tone="light" />
          <Swatch bg="var(--bg)" name="Background" hex="#F6F7F9" tone="light" />
          <Swatch bg="var(--rule)" name="Rule" hex="#E5E9EF" tone="light" />
          <Swatch bg="var(--teal-50)" name="Petrol 50" hex="#E8F2F2" tone="light" />
        </div>
      </Section>

      <Section eyebrow="02 / Tipografia" title="Type system" sub="General Sans (display + body) com pesos 400/500/600. JetBrains Mono para rótulos técnicos.">
        <TypeRow meta="Display" size="48 / 1.05" klass="type-display">
          Manipulação sem ruído.
        </TypeRow>
        <TypeRow meta="Heading 1" size="30 / 1.15" klass="type-h1">
          Pedidos do dia
        </TypeRow>
        <TypeRow meta="Heading 2" size="20 / 1.25" klass="type-h2">
          Pesagem · Bancada 02
        </TypeRow>
        <TypeRow meta="Body" size="15 / 1.6" klass="type-body">
          Sistema completo para farmácias de manipulação. Pesagem, rotulagem, controle de estoque e atendimento PDV em um só lugar.
        </TypeRow>
        <TypeRow meta="Small" size="12 / 1.5" klass="type-small">
          Última atualização há 2 minutos · Conferido por J. Lima
        </TypeRow>
        <TypeRow meta="Mono" size="12 / 1.5" klass="type-mono">
          ORD-2026-04-1183 · CAS 50-78-2
        </TypeRow>
      </Section>

      <Section eyebrow="03 / Tokens" title="Sistema" sub="Raio uniforme, três níveis de elevação, escala 4/8.">
        <div className="token-table">
          <TokenCell label="Radius / sm" value="4px" />
          <TokenCell label="Radius / md" value="6px" />
          <TokenCell label="Radius / lg" value="10px" />
          <TokenCell label="Radius / xl" value="14px" />
          <TokenCell label="Shadow / 1" value="0 1px 2px rgba(11,18,32,.04)" />
          <TokenCell label="Shadow / 2" value="0 8px 24px −8px rgba(11,18,32,.10)" />
          <TokenCell label="Spacing base" value="4 · 8 · 12 · 16 · 24 · 32" />
          <TokenCell label="Grid" value="12 col · 16px gutter" />
        </div>
      </Section>

      <Section eyebrow="04 / Componentes" title="Atomos" sub="Botão, input, badge, card, tabela. Mesma linguagem entre área pública e admin.">
        <div className="grid-2" style={{ marginBottom: 16 }}>
          <div className="card">
            <div className="card-eyebrow">Botões</div>
            <div className="row" style={{ marginTop: 14 }}>
              <button className="btn btn-primary">Confirmar fórmula</button>
              <button className="btn btn-secondary">Salvar rascunho</button>
              <button className="btn btn-ghost">Cancelar</button>
            </div>
          </div>
          <div className="card">
            <div className="card-eyebrow">Status</div>
            <div className="row" style={{ marginTop: 14 }}>
              <span className="badge badge-ok">Liberado</span>
              <span className="badge badge-warn">Em conferência</span>
              <span className="badge badge-danger">Bloqueado</span>
              <span className="badge badge-neutral">Rascunho</span>
            </div>
          </div>
        </div>

        <div className="card" style={{ marginBottom: 16 }}>
          <div className="card-eyebrow">Formulário</div>
          <div className="grid-2" style={{ marginTop: 14, gap: 16 }}>
            <div>
              <label className="input-label">Princípio ativo</label>
              <input className="input" defaultValue="Minoxidil" />
            </div>
            <div>
              <label className="input-label">Concentração</label>
              <input className="input" defaultValue="5%" />
            </div>
            <div>
              <label className="input-label">Forma farmacêutica</label>
              <input className="input" defaultValue="Solução capilar" />
            </div>
            <div>
              <label className="input-label">Volume</label>
              <input className="input" defaultValue="60 mL" />
            </div>
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th>Pedido</th>
              <th>Cliente</th>
              <th>Fórmula</th>
              <th>Status</th>
              <th className="num">Total</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td className="type-mono">ORD-1183</td>
              <td>Marina Rocha</td>
              <td>Minoxidil 5% · 60mL</td>
              <td><span className="badge badge-ok">Liberado</span></td>
              <td className="num">R$ 86,40</td>
            </tr>
            <tr>
              <td className="type-mono">ORD-1182</td>
              <td>Henrique Sá</td>
              <td>Bupropiona 150mg · 30 cáps</td>
              <td><span className="badge badge-warn">Conferência</span></td>
              <td className="num">R$ 124,00</td>
            </tr>
            <tr>
              <td className="type-mono">ORD-1181</td>
              <td>Clínica Vita</td>
              <td>Tretinoína 0,05% · 30g</td>
              <td><span className="badge badge-neutral">Rascunho</span></td>
              <td className="num">R$ 58,00</td>
            </tr>
          </tbody>
        </table>
      </Section>

      <Section eyebrow="05 / Tela aplicada" title="Dashboard admin" sub="Mesma área que hoje usa cards com border-left colorido e gradiente roxo no header. Recomposta com sidebar discreta e métricas tipográficas.">
        <div className="dash-screen">
          <aside className="dash-side">
            <div className="brand-mark">
              <div className="brand-glyph">o</div>
              <div className="brand-name" style={{ fontSize: 14 }}>Salvelle</div>
            </div>
            <div className="nav-section">Operação</div>
            <div className="nav-item active"><div className="nav-icon"/>Dashboard</div>
            <div className="nav-item"><div className="nav-icon"/>Pedidos</div>
            <div className="nav-item"><div className="nav-icon"/>Manipulação</div>
            <div className="nav-item"><div className="nav-icon"/>PDV</div>
            <div className="nav-section">Cadastro</div>
            <div className="nav-item"><div className="nav-icon"/>Fórmulas</div>
            <div className="nav-item"><div className="nav-icon"/>Estoque</div>
            <div className="nav-item"><div className="nav-icon"/>Clientes</div>
            <div className="nav-section">Sistema</div>
            <div className="nav-item"><div className="nav-icon"/>Equipe</div>
            <div className="nav-item"><div className="nav-icon"/>Ajustes</div>
          </aside>
          <main className="dash-main">
            <div className="dash-topline">
              <div>
                <h2>Bom dia, Douglas.</h2>
                <p>Sexta-feira, 28 de abril · 14 pedidos abertos.</p>
              </div>
              <div className="row">
                <button className="btn btn-secondary">Exportar</button>
                <button className="btn btn-primary">+ Nova fórmula</button>
              </div>
            </div>
            <div className="stat-grid">
              <div className="stat">
                <div className="stat-label">Pedidos hoje</div>
                <div className="stat-value">42</div>
                <div className="stat-delta">+12% vs. ontem</div>
              </div>
              <div className="stat">
                <div className="stat-label">Em manipulação</div>
                <div className="stat-value">14</div>
                <div className="stat-delta down">−3 desde manhã</div>
              </div>
              <div className="stat">
                <div className="stat-label">Faturamento</div>
                <div className="stat-value">R$ 8.420</div>
                <div className="stat-delta">+R$ 1.140</div>
              </div>
              <div className="stat">
                <div className="stat-label">Ticket médio</div>
                <div className="stat-value">R$ 92</div>
                <div className="stat-delta">+R$ 4</div>
              </div>
            </div>
            <table>
              <thead>
                <tr>
                  <th>Pedido</th>
                  <th>Cliente</th>
                  <th>Fórmula</th>
                  <th>Status</th>
                  <th className="num">Total</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td className="type-mono">ORD-1183</td>
                  <td>Marina Rocha</td>
                  <td>Minoxidil 5% · 60mL</td>
                  <td><span className="badge badge-ok">Liberado</span></td>
                  <td className="num">R$ 86,40</td>
                </tr>
                <tr>
                  <td className="type-mono">ORD-1182</td>
                  <td>Henrique Sá</td>
                  <td>Bupropiona 150mg · 30 cáps</td>
                  <td><span className="badge badge-warn">Conferência</span></td>
                  <td className="num">R$ 124,00</td>
                </tr>
                <tr>
                  <td className="type-mono">ORD-1181</td>
                  <td>Clínica Vita</td>
                  <td>Tretinoína 0,05% · 30g</td>
                  <td><span className="badge badge-neutral">Rascunho</span></td>
                  <td className="num">R$ 58,00</td>
                </tr>
              </tbody>
            </table>
          </main>
        </div>
      </Section>
    </div>
  );
}

window.ClinicalProposal = ClinicalProposal;
