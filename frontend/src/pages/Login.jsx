import { useState } from 'react';
import './Login.css';

export function Login({ onLogin }) {
  const [isRegistering, setIsRegistering] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setIsSubmitting(true);
    
    const endpoint = isRegistering ? '/api/register' : '/api/login';

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      
      const result = await response.json().catch(() => ({}));
      
      if (!response.ok) {
        setError(result.error || `The server returned ${response.status}.`);
        return;
      }
      
      sessionStorage.setItem('poll-token', result.token);
      onLogin(result.profile);
    } catch {
      setError('Could not reach the app server.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>{isRegistering ? 'Create an account' : 'Sign in'}</h1>
        <p className="login-description">
          {isRegistering ? 'Choose a username and password.' : 'Enter your credentials to continue.'}
        </p>
        
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
          autoComplete={isRegistering ? "new-password" : "current-password"}
          placeholder={isRegistering ? "At least 6 characters" : ""}
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />
        
        {error && <p className="login-error" role="alert">{error}</p>}
        
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting 
            ? (isRegistering ? 'Creating account…' : 'Signing in…') 
            : (isRegistering ? 'Register' : 'Continue')}
        </button>

        <p style={{ marginTop: '20px', fontSize: '13px', textAlign: 'center' }}>
          {isRegistering ? 'Already have an account? ' : 'Need an account? '}
          <button 
            type="button" 
            onClick={() => {
              setIsRegistering(!isRegistering);
              setError('');
            }}
            style={{ background: 'none', border: 'none', color: 'var(--accent)', padding: 0, margin: 0, cursor: 'pointer', display: 'inline', width: 'auto', fontWeight: 'bold' }}
          >
            {isRegistering ? 'Sign in' : 'Register'}
          </button>
        </p>
      </form>
    </main>
  );
}