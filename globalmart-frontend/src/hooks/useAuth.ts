import { useEffect, useState } from 'react';

export function useAuth() {
  const [token, setTokenState] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    window.api.getToken().then((t) => {
      setTokenState(t);
      setLoading(false);
    });
  }, []);

  const login = async (newToken: string) => {
    await window.api.setToken(newToken);
    setTokenState(newToken);
  };

  const logout = async () => {
    await window.api.logout();
    setTokenState(null);
  };

  return { token, loading, login, logout, isAuthenticated: !!token };
}
