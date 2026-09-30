import { useEffect, useState } from 'react';
import { CategorizedPoll } from './CategorizedPoll';
import { MiniFeed, FullFeed } from './PollFeed';
import { Login } from './Login';
import { PublicProfiles } from './PublicProfiles';
import './App.css';

function App() {
  const [username, setUsername] = useState(null);
  const [userId, setUserId] = useState(null);
  const [userPoints, setUserPoints] = useState(0);
  const [restoring, setRestoring] = useState(() => Boolean(sessionStorage.getItem('poll-username')));
  const [currentView, setCurrentView] = useState('creator');
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  // Fetch user data on load
  useEffect(() => {
    const savedUsername = sessionStorage.getItem('poll-username');
    if (!savedUsername) {
      return;
    }

    fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: savedUsername }),
    })
      .then((response) => {
        if (!response.ok) throw new Error('Could not restore username.');
        return response.json();
      })
      .then((data) => {
        setUsername(data.username);
        setUserId(data.id);
        setUserPoints(data.pointsBalance);
      })
      .catch(() => sessionStorage.removeItem('poll-username'))
      .finally(() => setRestoring(false));
  }, []);


  const handleLogin = (user) => {
    sessionStorage.setItem('poll-username', user.username);
    setUsername(user.username);
    setUserId(user.id);
    setUserPoints(user.pointsBalance);
  };

  if (restoring) return null;
  if (!username) return <Login onLogin={handleLogin} />;

  return (
    <div style={{ paddingBottom: '70px' }}>
      <header className="app-user-bar">
        <span>Signed in as <strong>{username}</strong></span>
        <button type="button" onClick={() => setCurrentView('profiles')}>
          Public profiles
        </button>
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

      {currentView === 'profiles' ? (
        <PublicProfiles onBack={() => setCurrentView('creator')} />
      ) : currentView === 'creator' ? (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '40px', padding: '20px', alignItems: 'flex-start' }}>
          <CategorizedPoll
            username={username}
            onPollCreated={() => setRefreshTrigger((previous) => previous + 1)}
          />
          <MiniFeed
            onNavigate={() => setCurrentView('feed')}
            refreshTrigger={refreshTrigger}
            username={username}
            userId={userId}
          />
        </div>
      ) : (
        <FullFeed
          onNavigate={() => setCurrentView('creator')}
          onVoteCast={(newPoints) => setUserPoints(newPoints)}
          username={username}
          userId={userId}
        />
      )}


      <div style={{ 
        position: 'fixed', 
        bottom: 0, 
        left: 0, 
        width: '100%', 
        background: '#fff', 
        borderTop: '1px solid var(--border)', 
        padding: '15px 20px', 
        textAlign: 'center', 
        fontWeight: 'bold',
        boxShadow: '0 -4px 12px rgba(0,0,0,0.05)',
        zIndex: 1000
      }}>
        Points: <span style={{ color: '#4caf50', fontSize: '18px' }}>{userPoints}</span>
      </div>
    </div>
  );
}

export default App;
