import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App';

const container = document.getElementById('root')!;
createRoot(container).render(<App />);
