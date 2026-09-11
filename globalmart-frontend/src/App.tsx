import { HashRouter, Routes, Route, Navigate } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import Login from './pages/Login';
import Pos from './pages/Pos';
import Admin from './pages/Admin';

export default function App() {
  return (
    <HashRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<AppLayout />}>
          <Route path="/pos" element={<Pos />} />
          <Route path="/admin/*" element={<Admin />} />
        </Route>
        {/* Redirige la raíz al login por defecto */}
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </HashRouter>
  );
}
