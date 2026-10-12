import React from 'react';
import { createRoot } from 'react-dom/client';
import { HashRouter } from 'react-router-dom';
import Pos from '../src/pages/Pos';
import '../src/index.css';

// Renderer real; el puente de sesión se inyecta desde Playwright.
createRoot(document.getElementById('root')!).render(<HashRouter><Pos /></HashRouter>);
