import { useState } from 'react';
import './Login.css';

export function Login({ onLogin }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);
    try {
      const response = await fetch('/api/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(
          result.error ||
            `The server returned ${response.status}. Make sure the .NET server is running.`,
        );
        return;
      }
      onLogin(result);
    } catch {
      setError('Could not reach the app server. Make sure both servers are running.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Sign in</h1>
        <p className="login-description">Enter your username to continue.</p>
        <label htmlFor="username">Username</label>
        <input
          id="username"
          autoComplete="username"
          maxLength={40}
          placeholder="e.g. Tom"
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          required
        />
        <label htmlFor="password">Password</label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />
        {error && <p className="login-error" role="alert">{error}</p>}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Continue'}
        </button>
      </form>
    </main>
  );
}
