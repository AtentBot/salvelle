/* Shared icons component — inline SVGs we can reuse across screens */
function Icon({ name, size = 16, ...rest }) {
  const cls = `ic${size === 20 ? ' ic-lg' : size === 24 ? ' ic-xl' : ''}`;
  const props = { className: cls, viewBox: "0 0 24 24", ...rest };
  switch (name) {
    case 'dashboard': return <svg {...props}><rect x="3" y="3" width="7" height="9"/><rect x="14" y="3" width="7" height="5"/><rect x="14" y="12" width="7" height="9"/><rect x="3" y="16" width="7" height="5"/></svg>;
    case 'orders': return <svg {...props}><path d="M9 3h6l1 2h3v15a2 2 0 01-2 2H7a2 2 0 01-2-2V5h3l1-2z"/><path d="M9 12l2 2 4-4"/></svg>;
    case 'flask': return <svg {...props}><path d="M9 3v6L4 19a2 2 0 002 2h12a2 2 0 002-2L15 9V3"/><path d="M8 3h8"/><path d="M7 14h10"/></svg>;
    case 'pdv': return <svg {...props}><rect x="3" y="6" width="18" height="13" rx="2"/><path d="M7 11h10"/><path d="M7 15h6"/><path d="M3 6l3-3h12l3 3"/></svg>;
    case 'package': return <svg {...props}><path d="M21 8l-9-5-9 5 9 5 9-5z"/><path d="M3 8v8l9 5 9-5V8"/><path d="M12 13v8"/></svg>;
    case 'people': return <svg {...props}><circle cx="9" cy="8" r="3"/><path d="M3 21v-2a4 4 0 014-4h4a4 4 0 014 4v2"/><circle cx="17" cy="7" r="2.5"/><path d="M21 21v-1.5a3 3 0 00-3-3"/></svg>;
    case 'box': return <svg {...props}><rect x="3" y="7" width="18" height="14" rx="2"/><path d="M3 10h18"/><path d="M8 14h2"/></svg>;
    case 'warehouse': return <svg {...props}><path d="M3 9l9-5 9 5v11H3V9z"/><rect x="8" y="13" width="8" height="7"/></svg>;
    case 'scan': return <svg {...props}><path d="M3 7V5a2 2 0 012-2h2"/><path d="M21 7V5a2 2 0 00-2-2h-2"/><path d="M21 17v2a2 2 0 01-2 2h-2"/><path d="M3 17v2a2 2 0 002 2h2"/><path d="M7 12h10"/></svg>;
    case 'settings': return <svg {...props}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 00.3 1.8l.1.1a2 2 0 11-2.8 2.8l-.1-.1a1.7 1.7 0 00-1.8-.3 1.7 1.7 0 00-1 1.5V21a2 2 0 11-4 0v-.1A1.7 1.7 0 008 19.4a1.7 1.7 0 00-1.8.3l-.1.1a2 2 0 11-2.8-2.8l.1-.1a1.7 1.7 0 00.3-1.8 1.7 1.7 0 00-1.5-1H2a2 2 0 110-4h.1A1.7 1.7 0 003.6 8a1.7 1.7 0 00-.3-1.8l-.1-.1a2 2 0 112.8-2.8l.1.1a1.7 1.7 0 001.8.3H8a1.7 1.7 0 001-1.5V2a2 2 0 114 0v.1a1.7 1.7 0 001 1.5 1.7 1.7 0 001.8-.3l.1-.1a2 2 0 112.8 2.8l-.1.1a1.7 1.7 0 00-.3 1.8V8a1.7 1.7 0 001.5 1H21a2 2 0 110 4h-.1a1.7 1.7 0 00-1.5 1z"/></svg>;
    case 'search': return <svg {...props}><circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/></svg>;
    case 'plus': return <svg {...props}><path d="M12 5v14"/><path d="M5 12h14"/></svg>;
    case 'arrow-up': return <svg {...props}><path d="M12 19V5"/><path d="M5 12l7-7 7 7"/></svg>;
    case 'arrow-down': return <svg {...props}><path d="M12 5v14"/><path d="M5 12l7 7 7-7"/></svg>;
    case 'arrow-right': return <svg {...props}><path d="M5 12h14"/><path d="M12 5l7 7-7 7"/></svg>;
    case 'check': return <svg {...props}><path d="M5 12l5 5L20 7"/></svg>;
    case 'x': return <svg {...props}><path d="M6 6l12 12M18 6L6 18"/></svg>;
    case 'bell': return <svg {...props}><path d="M18 8a6 6 0 10-12 0c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M14 21a2 2 0 11-4 0"/></svg>;
    case 'download': return <svg {...props}><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><path d="M7 10l5 5 5-5"/><path d="M12 15V3"/></svg>;
    case 'filter': return <svg {...props}><path d="M3 5h18l-7 9v6l-4-2v-4L3 5z"/></svg>;
    case 'calendar': return <svg {...props}><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/></svg>;
    case 'clock': return <svg {...props}><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>;
    case 'shield': return <svg {...props}><path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6l8-3z"/></svg>;
    case 'sparkle': return <svg {...props}><path d="M12 3v4M12 17v4M3 12h4M17 12h4M5 5l3 3M16 16l3 3M5 19l3-3M16 8l3-3"/></svg>;
    case 'menu-dots': return <svg {...props}><circle cx="12" cy="6" r="1.4"/><circle cx="12" cy="12" r="1.4"/><circle cx="12" cy="18" r="1.4"/></svg>;
    case 'chevron-right': return <svg {...props}><path d="M9 6l6 6-6 6"/></svg>;
    case 'chevron-down': return <svg {...props}><path d="M6 9l6 6 6-6"/></svg>;
    case 'home': return <svg {...props}><path d="M3 11l9-7 9 7"/><path d="M5 10v10h14V10"/></svg>;
    case 'circle': return <svg {...props}><circle cx="12" cy="12" r="9"/></svg>;
    case 'dot': return <svg {...props}><circle cx="12" cy="12" r="3" fill="currentColor"/></svg>;
    case 'lock': return <svg {...props}><rect x="4" y="11" width="16" height="10" rx="2"/><path d="M8 11V7a4 4 0 018 0v4"/></svg>;
    case 'arrow-left': return <svg {...props}><path d="M19 12H5"/><path d="M12 5l-7 7 7 7"/></svg>;
    case 'dollar': return <svg {...props}><path d="M12 3v18"/><path d="M17 7H9.5a2.5 2.5 0 000 5h5a2.5 2.5 0 010 5H6"/></svg>;
    case 'credit-card': return <svg {...props}><rect x="3" y="6" width="18" height="13" rx="2"/><path d="M3 11h18"/><path d="M7 16h3"/></svg>;
    case 'qr': return <svg {...props}><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><path d="M14 14h3v3M21 14v3M14 21h3M21 17v4"/></svg>;
    case 'receipt': return <svg {...props}><path d="M5 3v18l3-2 3 2 3-2 3 2 3-2V3z"/><path d="M9 8h6M9 12h6M9 16h4"/></svg>;
    case 'edit': return <svg {...props}><path d="M11 4H5a2 2 0 00-2 2v13a2 2 0 002 2h13a2 2 0 002-2v-6"/><path d="M18 2l4 4-12 12H6v-4z"/></svg>;
    case 'trash': return <svg {...props}><path d="M3 6h18"/><path d="M8 6V4a2 2 0 012-2h4a2 2 0 012 2v2"/><path d="M5 6v14a2 2 0 002 2h10a2 2 0 002-2V6"/></svg>;
    case 'archive': return <svg {...props}><rect x="3" y="3" width="18" height="5" rx="1"/><path d="M5 8v11a2 2 0 002 2h10a2 2 0 002-2V8"/><path d="M10 13h4"/></svg>;
    case 'eye': return <svg {...props}><path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/></svg>;
    case 'bolt': return <svg {...props}><path d="M13 2L4 14h7l-1 8 9-12h-7l1-8z"/></svg>;
    case 'truck': return <svg {...props}><rect x="2" y="6" width="13" height="10" rx="1"/><path d="M15 9h4l3 4v3h-7"/><circle cx="6" cy="18" r="2"/><circle cx="17" cy="18" r="2"/></svg>;
    default: return <svg {...props}/>;
  }
}

window.Icon = Icon;
