import { useEffect, useState } from 'react';
import './FollowingFeed.css';

export function FollowingFeed({ onBack }) {
  const [votes, setVotes] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    let isActive = true;

    const loadVotes = async () => {
      try {
        const response = await fetch('/api/users/following/votes', {
          headers: { Authorization: `Bearer ${sessionStorage.getItem('poll-token')}` },
        });
        if (!response.ok) throw new Error('Could not load the following feed.');

        const data = await response.json();
        if (isActive) {
          setVotes(data);
          setError('');
        }
      } catch {
        if (isActive) setError('Could not load the following feed. Please try again.');
      }
    };

    loadVotes();
    const refreshInterval = setInterval(loadVotes, 10000);

    return () => {
      isActive = false;
      clearInterval(refreshInterval);
    };
  }, []);

  return (
    <main className="following-page">
      <div className="following-heading">
        <div>
          <h1>Following</h1>
          <p>Recent selections from users you follow, shown after polls close.</p>
        </div>
        <button type="button" onClick={onBack}>Back</button>
      </div>

      {error && <p className="following-error" role="alert">{error}</p>}
      {!error && votes.length === 0 && (
        <p className="following-empty">No resolved votes yet. Follow users from their public profiles.</p>
      )}

      <div className="following-list">
        {votes.map((vote, index) => (
          <article className="following-card" key={`${vote.pollId}-${vote.username}-${index}`}>
            <div className="following-card-heading">
              <div>
                <strong>{vote.username}</strong>
                <span> voted in {vote.category}</span>
              </div>
              <time dateTime={vote.pollClosedAt}>{new Date(vote.pollClosedAt).toLocaleString()}</time>
            </div>
            <h2>{vote.question}</h2>
            <p className="following-selection">
              Selected: <strong>{vote.selections.join(', ') || 'No selection'}</strong>
            </p>
            <span className={`following-result ${vote.wasCorrect ? 'is-correct' : 'is-incorrect'}`}>
              {vote.wasCorrect ? 'Correct prediction' : 'Incorrect prediction'}
            </span>
          </article>
        ))}
      </div>
    </main>
  );
}
