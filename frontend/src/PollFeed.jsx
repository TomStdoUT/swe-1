import { useState, useEffect } from 'react';

// --- SHARED TIMER COMPONENT ---
function TimerAndProgress({ createdAt, closingTime }) {
  const [timeLeft, setTimeLeft] = useState('');
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const calculateTime = () => {
      const now = new Date().getTime();
      const closing = new Date(closingTime).getTime();
      const created = new Date(createdAt).getTime();

      const totalDuration = closing - created;
      const remaining = closing - now;

      if (remaining <= 0) {
        setTimeLeft('Baigta (Closed)');
        setProgress(100);
        return;
      }

      const elapsed = now - created;
      setProgress(Math.min(100, Math.max(0, (elapsed / totalDuration) * 100)));

      const hours = Math.floor((remaining / (1000 * 60 * 60)) % 24);
      const minutes = Math.floor((remaining / 1000 / 60) % 60);
      const seconds = Math.floor((remaining / 1000) % 60);
      setTimeLeft(`${hours}h ${minutes}m ${seconds}s left`);
    };

    calculateTime();
    const interval = setInterval(calculateTime, 1000);
    return () => clearInterval(interval);
  }, [closingTime, createdAt]);

  return (
    <div style={{ marginBottom: '15px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', marginBottom: '8px' }}>
        <span>Time left:</span>
        <strong>{timeLeft}</strong>
      </div>
      <div style={{ width: '100%', height: '8px', background: 'var(--code-bg)', borderRadius: '4px', overflow: 'hidden' }}>
        <div style={{ width: `${progress}%`, height: '100%', background: progress === 100 ? '#dc2626' : 'var(--accent)', transition: 'width 1s linear' }}></div>
      </div>
    </div>
  );
}

// --- MINI FEED (Side-by-side version) ---
export function MiniFeed({ onNavigate, refreshTrigger }) {
  const [polls, setPolls] = useState([]);

  useEffect(() => {
    fetch('/api/polls').then(res => res.json()).then(data => setPolls(data));
  }, [refreshTrigger]);

  const handleDelete = async (pollId) => {
    if (window.confirm('Are you sure you want to delete this poll?')) {
      try {
        const response = await fetch(`/api/polls/${pollId}`, { method: 'DELETE' });
        if (response.ok) {
          setPolls(polls.filter(p => p.id !== pollId));
        } else {
          const data = await response.json();
          alert(data.error);
        }
      } catch (error) {
        alert('Klaida trinant apklausą.');
      }
    }
  };

  return (
    <div style={{ width: '100%', maxWidth: '400px', display: 'flex', flexDirection: 'column' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
        <h2>Poll side feed</h2>
        <button onClick={onNavigate} style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}>
          Go to Feed
        </button>
      </div>
      <div style={{ flex: 1, overflowY: 'auto', maxHeight: '75vh', paddingRight: '10px' }}>
        {polls.map(poll => (
          <div key={poll.id} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '16px', marginBottom: '16px', position: 'relative' }}>
            
            {/* Delete Button (Top Right) */}
            <button 
              onClick={() => handleDelete(poll.id)}
              style={{ position: 'absolute', top: '10px', right: '10px', background: '#dc2626', color: '#fff', border: 'none', borderRadius: '4px', padding: '4px 8px', cursor: 'pointer', fontSize: '12px' }}
            >
              Delete
            </button>

            <h3 style={{ margin: '0 0 10px 0', fontSize: '18px', paddingRight: '50px' }}>{poll.question}</h3>
            <TimerAndProgress createdAt={poll.createdAt} closingTime={poll.closingTime} />
          </div>
        ))}
      </div>
    </div>
  );
}


// --- FULL FEED (Full page version with voting) ---
export function FullFeed({ onNavigate, onVoteCast }) {
  const [polls, setPolls] = useState([]);

  useEffect(() => {
    fetch('/api/polls').then(res => res.json()).then(data => setPolls(data));
  }, []);

  const handleVote = async (pollId, optionIndex, bettedPoints) => {
    try {
      const response = await fetch(`/api/polls/${pollId}/vote`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ optionIndex, bettedPoints })
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.error);
      
      alert('Vote accepted!');
      onVoteCast(data.remainingPoints); 
      
    } catch (error) {
      alert(`Klaida: ${error.message}`);
    }
  };

  const handleDelete = async (pollId) => {
    if (window.confirm('Are you sure you want to delete this poll? All user votes will be lost.')) {
      try {
        const response = await fetch(`/api/polls/${pollId}`, { method: 'DELETE' });
        if (response.ok) {
          setPolls(polls.filter(p => p.id !== pollId));
        } else {
          const data = await response.json();
          alert(data.error);
        }
      } catch (error) {
        alert('Klaida trinant apklausą.');
      }
    }
  };

  return (
    <div style={{ padding: '40px', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '30px' }}>
        <h1>Poll feed</h1>
        <button onClick={onNavigate} style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}>
          ← Back to creation
        </button>
      </div>

      {polls.map(poll => (
        <FullPollCard key={poll.id} poll={poll} onVote={handleVote} onDelete={handleDelete} />
      ))}
    </div>
  );
}

// Individual Full Poll Card Logic
function FullPollCard({ poll, onVote, onDelete }) {
  const [selectedOption, setSelectedOption] = useState(null);
  const [bet, setBet] = useState(0);

  return (
    <div style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '24px', marginBottom: '24px', background: 'var(--bg)', textAlign: 'left', position: 'relative' }}>
      
      {/* Delete Button (Top Right) */}
      <button 
        onClick={() => onDelete(poll.id)}
        style={{ position: 'absolute', top: '24px', right: '24px', background: '#dc2626', color: '#fff', border: 'none', borderRadius: '4px', padding: '6px 12px', cursor: 'pointer', fontWeight: 'bold' }}
      >
        Delete
      </button>

      <h2 style={{ marginTop: 0, paddingRight: '80px' }}>{poll.question}</h2>
      <p style={{ fontSize: '14px', color: 'var(--text)' }}>Kategorija: {poll.category}</p>
      
      <TimerAndProgress createdAt={poll.createdAt} closingTime={poll.closingTime} />

      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', margin: '20px 0' }}>
        {poll.options.map((opt, index) => (
          <label key={index} style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: 'pointer' }}>
            <input 
              type="radio" 
              name={`poll-${poll.id}`} 
              checked={selectedOption === index}
              onChange={() => setSelectedOption(index)} 
            />
            {opt.text}
          </label>
        ))}
      </div>

      <div style={{ display: 'flex', gap: '15px', alignItems: 'center' }}>
        <input 
          type="number" 
          min="0" 
          placeholder="Statomi taškai (0)" 
          value={bet}
          onChange={(e) => setBet(Math.max(0, parseInt(e.target.value) || 0))}
          style={{ padding: '8px', width: '150px' }}
        />
        <button 
          onClick={() => onVote(poll.id, selectedOption, bet)}
          disabled={selectedOption === null}
          style={{ padding: '8px 24px', background: 'var(--accent)', color: '#fff', border: 'none', borderRadius: '4px', cursor: selectedOption === null ? 'not-allowed' : 'pointer' }}
        >
          Balsuoti
        </button>
      </div>
    </div>
  );
}