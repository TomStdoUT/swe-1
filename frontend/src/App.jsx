import { useEffect, useState } from 'react';
import { CategorizedPoll } from './CategorizedPoll';
import { MiniFeed, FullFeed } from './PollFeed';
import { Login } from './Login';
import './App.css';

function App() {
  const [username, setUsername] = useState(null);
  const [restoring, setRestoring] = useState(() => Boolean(sessionStorage.getItem('poll-username')));
  
  // Controls which page we are on: 'creator' or 'feed'
  const [currentView, setCurrentView] = useState('creator');

  useEffect(() => {
    const savedUsername = sessionStorage.getItem('poll-username');
    if (!savedUsername) return;

    fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: savedUsername }),
    })
      .then((response) => {
        if (!response.ok) throw new Error('Could not restore username.');
        setUsername(savedUsername);
      })
      .catch(() => sessionStorage.removeItem('poll-username'))
      .finally(() => setRestoring(false));
  }, []);

  const handleLogin = (name) => {
    sessionStorage.setItem('poll-username', name);
    setUsername(name);
  };

  if (restoring) return null;
  if (!username) return <Login onLogin={handleLogin} />;

  return (
    <div>
      <header className="app-user-bar">
        <span>Signed in as <strong>{username}</strong></span>
        <button
          type="button"
          onClick={() => {
            sessionStorage.removeItem('poll-username');
            setUsername(null);
          }}
        >
          Log out
        </button>
      </header>

      {/* RENDER LOGIC: Switch between pages based on state */}
      {currentView === 'creator' ? (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '40px', padding: '20px', alignItems: 'flex-start' }}>
          <CategorizedPoll />
          {/* Pass the navigation function to the MiniFeed */}
          <MiniFeed onNavigate={() => setCurrentView('feed')} />
        </div>
      ) : (
        <FullFeed onNavigate={() => setCurrentView('creator')} />
      )}
    </div>
  );
}

export default App;