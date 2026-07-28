/* Brand mark, navbar e sidebar reutilizáveis — Salvelle */
function SalvelleGlyph({ size = 32, color = 'var(--petrol-500)' }) {
  // Monograma S (Variante A — primária)
  return (
    <svg width={size} height={size} viewBox="0 0 88 88" fill="none" aria-hidden="true">
      <text x="44" y="68" text-anchor="middle" font-family="'Cormorant Garamond',serif" font-weight="600" font-size="80" fill={color}>S</text>
    </svg>
  );
}
function Brand({ size = 'md', href = 'index.html' }) {
  const sz = size === 'lg' ? 40 : size === 'sm' ? 26 : 32;
  const fs = size === 'lg' ? 26 : size === 'sm' ? 16 : 19;
  return (
    <a href={href} className="brand" style={{ fontSize: fs }}>
      <span className="brand-glyph" style={{ width: sz, height: sz }}>
        <SalvelleGlyph size={sz} />
      </span>
      <span className="brand-name">Salvelle</span>
    </a>
  );
}
window.SalvelleGlyph = SalvelleGlyph;

function Sidebar({ active }) {
  const items = [
    { sec: 'Operação', list: [
      { id: 'dashboard', label: 'Dashboard', icon: 'dashboard' },
      { id: 'orders', label: 'Pedidos', icon: 'orders', count: 14 },
      { id: 'manipulation', label: 'Manipulação', icon: 'flask', count: 6 },
      { id: 'pdv', label: 'PDV', icon: 'pdv' },
      { id: 'ocr', label: 'OCR de receita', icon: 'scan' },
    ]},
    { sec: 'Cadastro', list: [
      { id: 'formulas', label: 'Fórmulas', icon: 'package' },
      { id: 'inventory', label: 'Estoque', icon: 'warehouse' },
      { id: 'purchases', label: 'Pedidos de compra', icon: 'box' },
      { id: 'customers', label: 'Clientes', icon: 'people' },
    ]},
    { sec: 'Sistema', list: [
      { id: 'employees', label: 'Equipe', icon: 'people' },
      { id: 'settings', label: 'Ajustes', icon: 'settings' },
    ]},
  ];
  return (
    <aside className="sidebar">
      <div style={{ padding: '4px 8px 16px' }}>
        <Brand />
      </div>
      {items.map((sec, i) => (
        <React.Fragment key={i}>
          <div className="sidebar-section">{sec.sec}</div>
          {sec.list.map(it => (
            <a key={it.id} href={it.id === 'dashboard' ? 'dashboard.html' : it.id === 'orders' ? 'orders.html' : it.id === 'manipulation' ? 'manipulation.html' : it.id === 'pdv' ? 'pdv.html' : it.id === 'ocr' ? 'ocr.html' : it.id === 'employees' ? 'employees.html' : '#'}
               className={`nav-item ${active === it.id ? 'active' : ''}`}>
              <Icon name={it.icon} />
              <span>{it.label}</span>
              {it.count != null && <span className="nav-count">{it.count}</span>}
            </a>
          ))}
        </React.Fragment>
      ))}
    </aside>
  );
}

function Topbar({ title, subtitle, right }) {
  return (
    <header className="topbar">
      <div className="row" style={{ gap: 24 }}>
        <div className="topbar-search input-group">
          <span className="input-icon"><Icon name="search" /></span>
          <input className="input" placeholder="Buscar pedidos, clientes, fórmulas… ⌘K" style={{ paddingLeft: 36 }} />
        </div>
      </div>
      <div className="row" style={{ gap: 12 }}>
        <button className="btn btn-secondary btn-sm"><Icon name="bell" />Notificações</button>
        <div className="row" style={{ gap: 8 }}>
          <div style={{ width: 32, height: 32, borderRadius: '50%', background: 'var(--petrol-500)', color: '#fff', display: 'grid', placeItems: 'center', fontSize: 12, fontWeight: 600 }}>DB</div>
          <div style={{ lineHeight: 1.2, fontSize: 12 }}>
            <div style={{ fontWeight: 500, color: 'var(--ink)' }}>Douglas B.</div>
            <div className="muted">Farmacêutico RT</div>
          </div>
        </div>
      </div>
    </header>
  );
}

function PublicNav({ active }) {
  return (
    <nav className="navbar">
      <Brand />
      <div className="navbar-links">
        <a className={`navbar-link ${active === 'home' ? 'active' : ''}`} href="index.html">Produto</a>
        <a className={`navbar-link ${active === 'features' ? 'active' : ''}`} href="index.html#features">Recursos</a>
        <a className={`navbar-link ${active === 'pricing' ? 'active' : ''}`} href="pricing.html">Preços</a>
        <a className="navbar-link" href="#">Documentação</a>
      </div>
      <div className="row" style={{ gap: 12 }}>
        <a className="btn btn-ghost btn-sm" href="login.html">Entrar</a>
        <a className="btn btn-primary btn-sm" href="signup.html">Começar grátis</a>
      </div>
    </nav>
  );
}

function PageHead({ title, subtitle, actions, breadcrumb }) {
  return (
    <div className="page-head">
      <div>
        {breadcrumb && <div className="eyebrow" style={{ marginBottom: 6 }}>{breadcrumb}</div>}
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}
      </div>
      {actions && <div className="row">{actions}</div>}
    </div>
  );
}

window.Brand = Brand;
window.Sidebar = Sidebar;
window.Topbar = Topbar;
window.PublicNav = PublicNav;
window.PageHead = PageHead;
