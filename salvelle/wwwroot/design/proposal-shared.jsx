/* global React */
const { useState } = React;

function Section({ eyebrow, title, sub, children, style }) {
  return (
    <section style={style}>
      {eyebrow && <div className="section-eyebrow">{eyebrow}</div>}
      {title && <h2 className="section-title">{title}</h2>}
      {sub && <p className="section-sub">{sub}</p>}
      {children}
    </section>
  );
}

function Swatch({ bg, name, hex, tone }) {
  return (
    <div className="swatch">
      <div className={`swatch-chip ${tone || ''}`} style={{ background: bg }}>
        {hex}
      </div>
      <div className="swatch-name">{name}</div>
      <div className="swatch-hex">{hex}</div>
    </div>
  );
}

function TypeRow({ meta, size, children, klass }) {
  return (
    <div className="type-spec">
      <div className="type-meta">{meta}</div>
      <div className={klass}>{children}</div>
      <div className="type-size">{size}</div>
    </div>
  );
}

function TokenCell({ label, value }) {
  return (
    <div className="token-cell">
      <div className="token-cell-label">{label}</div>
      <div className="token-cell-value">{value}</div>
    </div>
  );
}

window.Section = Section;
window.Swatch = Swatch;
window.TypeRow = TypeRow;
window.TokenCell = TokenCell;
