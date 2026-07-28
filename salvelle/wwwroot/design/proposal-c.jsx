/* global React, Section, Swatch, TypeRow, TokenCell */

function CompoundProposal() {
  return (
    <div className="proposal compound">
      <header className="proposal-header">
        <div>
          <div className="brand-mark">
            <div className="brand-glyph">℞</div>
            <div className="brand-name">Salvelle <span className="ver">v2.0</span></div>
          </div>
          <h1 className="proposal-title">
            Proposta C — <span className="accent">Compound</span>
          </h1>
          <p className="proposal-tagline">
            Identidade técnico-minimalista. Grotesque com mono nos rótulos,
            superfícies brancas, acento âmbar usado pontualmente. Sensação de
            ferramenta densa e precisa, no espírito de Linear ou Stripe Atlas.
          </p>
        </div>
        <div className="proposal-meta">
          <div><strong>DIRECTION-C</strong> · 03</div>
          <div>Amber / Graphite</div>
          <div>Geist · Geist Mono</div>
        </div>
      </header>

      <Section eyebrow="paleta" title="Cor" sub="Quase preto + branco, com âmbar como spot. Status em soft-fill com borda interna sutil — nunca como decoração.">
        <div className="grid-4" style={{ marginBottom: 24 }}>
          <Swatch bg="var(--ink)" name="Ink — Primária" hex="#0A0A0A" tone="dark" />
          <Swatch bg="var(--amber)" name="Amber — Spot" hex="#C2410C" tone="dark" />
          <Swatch bg="var(--ink-2)" name="Ink 2" hex="#262626" tone="dark" />
          <Swatch bg="var(--ink-3)" name="Ink 3" hex="#525252" tone="dark" />
          <Swatch bg="var(--surface)" name="Surface" hex="#FFFFFF" tone="light" />
          <Swatch bg="var(--bg)" name="Background" hex="#FAFAFA" tone="light" />
          <Swatch bg="var(--bg-subtle)" name="Subtle" hex="#F4F4F4" tone="light" />
          <Swatch bg="var(--rule)" name="Rule" hex="#EAEAEA" tone="light" />
        </div>
      </Section>

      <Section eyebrow="tipografia" title="Type system" sub="Geist (sans grotesque, peso 400/500). Geist Mono para IDs, dosagens, comandos, atalhos e qualquer dado técnico.">
        <TypeRow meta="display" size="44 / 1.05" klass="type-display">
          Manipule. Pese. Despache.
        </TypeRow>
        <TypeRow meta="h1" size="26 / 1.15" klass="type-h1">
          Fórmula 1183 · Minoxidil 5%
        </TypeRow>
        <TypeRow meta="h2" size="18 / 1.25" klass="type-h2">
          Pesagem · bancada 02
        </TypeRow>
        <TypeRow meta="body" size="14 / 1.55" klass="type-body">
          Tudo o que o farmacêutico precisa em um só lugar — fórmulas, pesagem, rótulos, controle de estoque e PDV.
        </TypeRow>
        <TypeRow meta="small" size="12 / 1.5" klass="type-small">
          Atualizado há 2 min · J. Lima
        </TypeRow>
        <TypeRow meta="mono" size="12 / 1.5" klass="type-mono">
          ORD-2026-04-1183 · CAS 50-78-2
        </TypeRow>
      </Section>

      <Section eyebrow="tokens" title="Sistema" sub="Raio pequeno e consistente, ruling 1px, sombra mínima. Densidade alta — bom para telas com muito dado.">
        <div className="token-table">
          <TokenCell label="radius/sm" value="3px" />
          <TokenCell label="radius/md" value="5px" />
          <TokenCell label="radius/lg" value="6px" />
          <TokenCell label="radius/xl" value="8px" />
          <TokenCell label="shadow/1" value="ring 1px rgba(0,0,0,.05)" />
          <TokenCell label="shadow/2" value="0 6px 16px −8px rgba(0,0,0,.12)" />
          <TokenCell label="spacing" value="2 · 4 · 6 · 8 · 12 · 16 · 24" />
          <TokenCell label="grid" value="12 col · 12px gutter" />
        </div>
      </Section>

      <Section eyebrow="componentes" title="Atomos" sub="Densidade controlada, atalhos sempre visíveis, mono para qualquer dado tabular.">
        <div className="grid-2" style={{ marginBottom: 16 }}>
          <div className="card">
            <div className="card-eyebrow">botões</div>
            <div className="row" style={{ marginTop: 12 }}>
              <button className="btn btn-primary">Confirmar fórmula</button>
              <button className="btn btn-secondary">Salvar rascunho</button>
              <button className="btn btn-ghost">Cancelar <span className="kbd">Esc</span></button>
            </div>
          </div>
          <div className="card">
            <div className="card-eyebrow">status</div>
            <div className="row" style={{ marginTop: 12 }}>
              <span className="badge badge-ok">liberado</span>
              <span className="badge badge-warn">conferência</span>
              <span className="badge badge-danger">bloqueado</span>
              <span className="badge badge-neutral">rascunho</span>
            </div>
          </div>
        </div>

        <div className="card" style={{ marginBottom: 16 }}>
          <div className="card-eyebrow">formulário</div>
          <div className="grid-2" style={{ marginTop: 12, gap: 16 }}>
            <div>
              <label className="input-label">princípio ativo</label>
              <input className="input" defaultValue="Minoxidil" />
            </div>
            <div>
              <label className="input-label">concentração</label>
              <input className="input" defaultValue="5%" />
            </div>
            <div>
              <label className="input-label">forma farmacêutica</label>
              <input className="input" defaultValue="Solução capilar" />
            </div>
            <div>
              <label className="input-label">volume</label>
              <input className="input" defaultValue="60 mL" />
            </div>
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th>pedido</th>
              <th>cliente</th>
              <th>fórmula</th>
              <th>status</th>
              <th className="num">total</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td className="num" style={{ textAlign: 'left' }}>ORD-1183</td>
              <td>Marina Rocha</td>
              <td>Minoxidil 5% · 60mL</td>
              <td><span className="badge badge-ok">liberado</span></td>
              <td className="num">86,40</td>
            </tr>
            <tr>
              <td className="num" style={{ textAlign: 'left' }}>ORD-1182</td>
              <td>Henrique Sá</td>
              <td>Bupropiona 150mg · 30 cáps</td>
              <td><span className="badge badge-warn">conferência</span></td>
              <td className="num">124,00</td>
            </tr>
            <tr>
              <td className="num" style={{ textAlign: 'left' }}>ORD-1181</td>
              <td>Clínica Vita</td>
              <td>Tretinoína 0,05% · 30g</td>
              <td><span className="badge badge-neutral">rascunho</span></td>
              <td className="num">58,00</td>
            </tr>
          </tbody>
        </table>
      </Section>

      <Section eyebrow="tela aplicada" title="Manipulação · fórmula" sub="Tela onde o farmacêutico passa a maior parte do tempo. Hoje é uma página de form Bootstrap. Recomposta como ferramenta densa, com totais e validações sempre à vista.">
        <div className="formula-screen">
          <div className="formula-bar">
            <div className="formula-bar-left">
              <span>ORD-1183</span>
              <span style={{ opacity: .35 }}>·</span>
              <strong>Minoxidil 5% — 60mL</strong>
              <span className="badge badge-warn" style={{ marginLeft: 8 }}>conferência</span>
            </div>
            <div className="row">
              <button className="btn btn-ghost">Histórico <span className="kbd">H</span></button>
              <button className="btn btn-secondary">Salvar <span className="kbd">⌘S</span></button>
              <button className="btn btn-primary">Liberar fórmula</button>
            </div>
          </div>
          <div className="formula-body">
            <div className="formula-list">
              <h3>Componentes</h3>
              <div className="formula-row">
                <div className="idx">01</div>
                <div>
                  <div className="ing-name">Minoxidil</div>
                  <div className="ing-cas">CAS 38304-91-5</div>
                </div>
                <div className="ing-qty">3,00 g</div>
                <div className="ing-pct">5,00%</div>
              </div>
              <div className="formula-row">
                <div className="idx">02</div>
                <div>
                  <div className="ing-name">Propilenoglicol</div>
                  <div className="ing-cas">CAS 57-55-6</div>
                </div>
                <div className="ing-qty">12,00 mL</div>
                <div className="ing-pct">20,00%</div>
              </div>
              <div className="formula-row">
                <div className="idx">03</div>
                <div>
                  <div className="ing-name">Álcool etílico 96°</div>
                  <div className="ing-cas">CAS 64-17-5</div>
                </div>
                <div className="ing-qty">18,00 mL</div>
                <div className="ing-pct">30,00%</div>
              </div>
              <div className="formula-row">
                <div className="idx">04</div>
                <div>
                  <div className="ing-name">Água purificada q.s.p.</div>
                  <div className="ing-cas">CAS 7732-18-5</div>
                </div>
                <div className="ing-qty">60,00 mL</div>
                <div className="ing-pct">45,00%</div>
              </div>
            </div>
            <div className="formula-aside">
              <div className="formula-aside-block">
                <div className="label">peso bruto</div>
                <div className="value">62,4 g</div>
                <div className="sub">balança · bancada 02</div>
              </div>
              <div className="formula-aside-block">
                <div className="label">custo insumos</div>
                <div className="value">R$ 14,82</div>
                <div className="sub">margem · 5,8x</div>
              </div>
              <div className="formula-aside-block">
                <div className="label">prazo de validade</div>
                <div className="value">90 dias</div>
                <div className="sub">após manipulação</div>
              </div>
              <div className="formula-aside-block">
                <div className="label">conferente</div>
                <div className="value" style={{ fontSize: 14 }}>—</div>
                <div className="sub">aguardando 2ª assinatura</div>
              </div>
            </div>
          </div>
        </div>
      </Section>
    </div>
  );
}

window.CompoundProposal = CompoundProposal;
