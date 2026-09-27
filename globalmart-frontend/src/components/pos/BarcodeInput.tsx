import React, { useState, useEffect } from 'react';

interface BarcodeInputProps {
  onSearch: (query: string) => void;
}

export default function BarcodeInput({ onSearch }: BarcodeInputProps) {
  const [value, setValue] = useState('');

  // Efecto Debounce de 300ms
  useEffect(() => {
    if (!value.trim()) return; // No busca si est vaco

    const timerId = setTimeout(() => {
      onSearch(value);
    }, 300);

    return () => {
      clearTimeout(timerId);
    };
  }, [value, onSearch]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      if (value.trim()) {
        onSearch(value);
      }
    }
  };

  return (
    <div style={{ position: 'relative', width: '100%', marginBottom: '1rem' }}>
      <div style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: '#6b7280', display: 'flex' }}>
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="11" cy="11" r="8"></circle>
          <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
        </svg>
      </div>
      <input
        type="text"
        placeholder="Escanea o busca un producto..."
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={handleKeyDown}
        style={{
          width: '100%',
          padding: '0.75rem 1rem 0.75rem 2.5rem',
          borderRadius: '8px',
          border: '1px solid #d1d5db',
          outline: 'none',
          fontSize: '1rem',
          boxSizing: 'border-box',
          boxShadow: '0 1px 2px rgba(0,0,0,0.05)',
          transition: 'all 0.2s ease',
        }}
        onFocus={(e) => {
            e.target.style.borderColor = '#1B4332';
            e.target.style.boxShadow = '0 0 0 3px rgba(27, 67, 50, 0.2)';
        }}
        onBlur={(e) => {
            e.target.style.borderColor = '#d1d5db';
            e.target.style.boxShadow = '0 1px 2px rgba(0,0,0,0.05)';
        }}
      />
    </div>
  );
}
