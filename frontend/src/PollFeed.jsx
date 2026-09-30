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
        setTimeLeft('Closed');
        setProgress(100);
        return;
      }

      const elapsed = now - created;
      setProgress(Math.min(100, Math.max(0, (elapsed / totalDuration) * 100)));

      const hours = Math.floor(remaining / (1000 * 60 * 60));
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

export function MiniFeed({ onNavigate, refreshTrigger }) {
  const [polls, setPolls] = useState([]);

  useEffect(() => {
    const username = sessionStorage.getItem('poll-username');
    fetch('/api/polls', {
      headers: { 'X-Username': username || '' }
    })
      .then(res => res.json())
      .then(data => setPolls(data));
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
        alert('Error deleting poll.');
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

// --- FULL FEED ---
export function FullFeed({ onNavigate, onVoteCast }) {
  const [polls, setPolls] = useState([]);

  const loadPolls = () => {
    const username = sessionStorage.getItem('poll-username');
    fetch('/api/polls', {
      headers: { 'X-Username': username || '' }
    })
      .then(res => res.json())
      .then(data => setPolls(data));
  };

  useEffect(() => {
    loadPolls();
  }, []);

  const handleVote = async (pollId, optionIndices, bettedPoints) => {
    try {
      const activeUsername = sessionStorage.getItem('poll-username'); // Read straight from session storage
      const response = await fetch(`/api/polls/${pollId}/vote`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'X-Username': activeUsername || '' 
        },
        body: JSON.stringify({ optionIndices, bettedPoints }) 
      });
      
      const data = await response.json();
      
      if (!response.ok) {
        const errorMessage = data.error || (data.errors && Object.values(data.errors).join(', ')) || 'Unknown error';
        throw new Error(errorMessage);
      }
      
      onVoteCast(data.remainingPoints); 
      loadPolls(); 
      return true;
    } catch (error) {
      alert(`Klaida: ${error.message}`);
      return false;
    }
  };

  const handleDelete = async (pollId) => {
    if (window.confirm('Are you sure you want to delete this polls? All your user choices will be lost.')) {
      try {
        const response = await fetch(`/api/polls/${pollId}`, { method: 'DELETE' });
        if (response.ok) {
          setPolls(polls.filter(p => p.id !== pollId));
        } else {
          const data = await response.json();
          alert(data.error);
        }
      } catch (error) {
        alert('Error deleting poll.');
      }
    }
  };

  return (
    <div style={{ padding: '40px', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '30px' }}>
        <h1>Visos Apklausos</h1>
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

// --- FULL POLL CARD ---
function FullPollCard({ poll, onVote, onDelete }) {
  const existingVote = poll.votes && poll.votes.length > 0 ? poll.votes[0] : null;
  const [selectedOptions, setSelectedOptions] = useState(existingVote ? existingVote.optionIndices : []);
  const [bet, setBet] = useState(existingVote ? existingVote.bettedPoints : 0);
  const [hasVoted, setHasVoted] = useState(!!existingVote);
  const [isVoting, setIsVoting] = useState(false);

  const [showComments, setShowComments] = useState(false);
  const [comments, setComments] = useState(poll.comments || []);
  const [commentText, setCommentText] = useState('');
  const [isSubmittingComment, setIsSubmittingComment] = useState(false);

  useEffect(() => {
    const currentVote = poll.votes && poll.votes.length > 0 ? poll.votes[0] : null;
    setSelectedOptions(currentVote ? currentVote.optionIndices : []);
    setBet(currentVote ? currentVote.bettedPoints : 0);
    setHasVoted(!!currentVote);
    setComments(poll.comments || []);
  }, [poll]);

  const handleAddComment = async (e) => {
    e.preventDefault();
    if (!commentText.trim()) return;

    setIsSubmittingComment(true);
    try {
        const response = await fetch(`/api/polls/${poll.id}/comments`, {        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: commentText.trim() }),
      });

      if (!response.ok) {
        const data = await response.json();
        alert(data.error || 'Failed to add comment.');
        return;
      }

      const newComment = await response.json();
      setComments((prev) => [...prev, newComment]);
      setCommentText('');
    } catch (error) {
      alert('Error submitting comment.');
    } finally {
      setIsSubmittingComment(false);
    }
  };
  
  const [isClosed, setIsClosed] = useState(
    new Date().getTime() >= new Date(poll.closingTime).getTime()
  );

  useEffect(() => {
    if (isClosed) return;
    const interval = setInterval(() => {
      if (new Date().getTime() >= new Date(poll.closingTime).getTime()) {
        setIsClosed(true);
        clearInterval(interval);
      }
    }, 1000);
    return () => clearInterval(interval);
  }, [poll.closingTime, isClosed]);

  const isLocked = isClosed || hasVoted;

  // NEW POPULARITY CALCULATION (Runs when the poll is closed)
  let isWinner = false;
  if (isClosed && hasVoted) {
    const voteCounts = new Array(poll.options.length).fill(0);
    if (poll.votes) {
      poll.votes.forEach(v => {
        v.optionIndices.forEach(idx => {
          if (idx >= 0 && idx < voteCounts.length) voteCounts[idx]++;
        });
      });
    }
    
    const maxVotes = Math.max(...voteCounts, 0);
    const winningIndices = voteCounts
      .map((count, index) => count === maxVotes && maxVotes > 0 ? index : -1)
      .filter(index => index !== -1);
      
    // If what they picked includes the community favorite, they win
    isWinner = selectedOptions.some(idx => winningIndices.includes(idx));
  }

  const toggleOption = (index) => {
    if (isLocked) return;
    if (poll.allowMultiple) {
      setSelectedOptions(prev => 
        prev.includes(index) ? prev.filter(i => i !== index) : [...prev, index]
      );
    } else {
      setSelectedOptions([index]);
    }
  };

  const submitVote = async () => {
    setIsVoting(true);
    const success = await onVote(poll.id, selectedOptions, bet);
    if (success) setHasVoted(true);
    setIsVoting(false);
  };

  return (
    <div style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '24px', marginBottom: '24px', background: 'var(--bg)', textAlign: 'left', position: 'relative' }}>
      
      <button 
        onClick={() => onDelete(poll.id)}
        style={{ position: 'absolute', top: '24px', right: '24px', background: '#dc2626', color: '#fff', border: 'none', borderRadius: '4px', padding: '6px 12px', cursor: 'pointer', fontWeight: 'bold' }}
      >
        Delete
      </button>

      <h2 style={{ marginTop: 0, paddingRight: '80px' }}>{poll.question}</h2>
      <p style={{ fontSize: '14px', color: 'var(--text)' }}>Category: {poll.category}</p>
      
      <TimerAndProgress createdAt={poll.createdAt} closingTime={poll.closingTime} />
      <div style={{ fontSize: '13px', color: 'var(--text)', marginBottom: '15px' }}>
        Total votes: <strong>{poll.totalVotesCount ?? 0}</strong>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', margin: '20px 0' }}>
        {poll.options.map((opt, index) => {
          if (isLocked && !selectedOptions.includes(index)) return null;

          return (
            <label 
              key={index} 
              style={{ display: 'flex', alignItems: 'center', gap: '10px', cursor: isLocked ? 'default' : 'pointer' }}
            >
              <input 
                type={poll.allowMultiple ? "checkbox" : "radio"} 
                checked={selectedOptions.includes(index)}
                onChange={() => toggleOption(index)} 
                disabled={isLocked}
              />
              {opt.text}
            </label>
          );
        })}
        {isLocked && selectedOptions.length === 0 && (
          <p style={{ color: 'var(--text)', fontStyle: 'italic', margin: 0 }}>You have not voted in this polls.</p>
        )}
      </div>

      <div style={{ display: 'flex', gap: '15px', alignItems: 'center' }}>
        <input 
          type="number" 
          min="0" 
          placeholder="Betted points (0)" 
          value={bet}
          onChange={(e) => setBet(Math.max(0, parseInt(e.target.value) || 0))}
          style={{ padding: '8px', width: '150px' }}
          disabled={isLocked}
        />
        <button 
          onClick={submitVote}
          disabled={selectedOptions.length === 0 || isLocked || isVoting}
          style={{ 
            padding: '8px 24px', 
            background: (selectedOptions.length === 0 || isLocked) ? 'var(--border)' : 'var(--accent)', 
            color: (selectedOptions.length === 0 || isLocked) ? 'var(--text)' : '#fff', 
            border: 'none', 
            borderRadius: '4px', 
            cursor: (selectedOptions.length === 0 || isLocked) ? 'not-allowed' : 'pointer' 
          }}
        >
          {hasVoted ? 'Vote accepted' : 'Vote'}
        </button>
      </div>

      {isClosed && hasVoted && (
        <div style={{ 
          marginTop: '20px', 
          padding: '12px', 
          borderRadius: '6px', 
          background: isWinner ? '#dcfce7' : '#fee2e2', 
          color: isWinner ? '#166534' : '#991b1b', 
          fontWeight: 'bold', 
          textAlign: 'center',
          border: `1px solid ${isWinner ? '#bbf7d0' : '#fecaca'}`
        }}>
          {isWinner 
            ? `Correct! Your answer won. You have won ${bet * 2} points!` 
            : `Incorrect. Your choice has lost. You have lost your betted points.`}
        </div>
      )}

    {/* Comments Toggle Button */}
      <div style={{ marginTop: '20px', borderTop: '1px solid var(--border)', paddingTop: '12px' }}>
        <button
          onClick={() => setShowComments(!showComments)}
          style={{
            background: 'none',
            border: 'none',
            color: 'var(--accent)',
            cursor: 'pointer',
            padding: 0,
            fontSize: '14px',
            fontWeight: 'bold',
          }}
        >
          {showComments ? 'Hide Comments' : `Comments (${comments.length})`}
        </button>

        {/* Collapsible Comments Section */}
        {showComments && (
          <div style={{ marginTop: '15px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '200px', overflowY: 'auto' }}>
              {comments.length === 0 ? (
                <p style={{ fontSize: '13px', color: 'var(--text)', fontStyle: 'italic' }}>No comments yet. Be the first to comment!</p>
              ) : (
                comments.map((c, i) => (
                  <div key={c.id || i} style={{ background: 'var(--code-bg)', padding: '8px 12px', borderRadius: '6px', fontSize: '14px' }}>
                    <strong>{c.authorName}: </strong>
                    <span>{c.content}</span>
                  </div>
                ))
              )}
            </div>

            <form onSubmit={handleAddComment} style={{ display: 'flex', gap: '8px', marginTop: '5px' }}>
              <input
                type="text"
                placeholder="Write a comment..."
                value={commentText}
                onChange={(e) => setCommentText(e.target.value)}
                style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid var(--border)' }}
              />
              <button
                type="submit"
                disabled={isSubmittingComment || !commentText.trim()}
                style={{ padding: '8px 16px', background: 'var(--accent)', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
              >
                Post
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
}