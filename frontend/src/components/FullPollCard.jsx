import { useState, useEffect } from 'react';
import { TimerAndProgress } from './TimerAndProgress';

export function FullPollCard({ poll, onVote, onDelete, userId, onPollFinished }) {
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
    
    if (!commentText.trim()) 
      return;

    setIsSubmittingComment(true);
    
    try {
      const response = await fetch(`/api/polls/${poll.id}/comments`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}`
        },
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
        
        if (onPollFinished) 
          onPollFinished();
      }
    }, 1000);
    
    return () => clearInterval(interval);
  }, [poll.closingTime, isClosed, onPollFinished]);

  const isLocked = isClosed || hasVoted;

  let isWinner = false;
  if (poll.isResolved && hasVoted) {
    isWinner = selectedOptions.some(idx => poll.winningOptions.includes(idx));
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
    
    if (success) 
      setHasVoted(true);
      
    setIsVoting(false);
  };

  return (
    <div style={{ border: poll.isDailyChallenge ? '2px solid var(--accent)' : '1px solid var(--border)', borderRadius: '8px', padding: '24px', marginBottom: '24px', background: 'var(--bg)', textAlign: 'left', position: 'relative' }}>      
      {poll.creatorId === userId && (
        <button
          onClick={() => onDelete(poll.id)}
          style={{ position: 'absolute', top: '24px', right: '24px', background: '#dc2626', color: '#fff', border: 'none', borderRadius: '4px', padding: '6px 12px', cursor: 'pointer', fontWeight: 'bold' }}
        >
          Delete
        </button>
      )}

    {poll.isDailyChallenge && (
            <div style={{ display: 'inline-block', background: 'var(--accent)', color: '#fff', fontSize: '12px', fontWeight: 'bold', padding: '3px 8px', borderRadius: '4px', marginBottom: '8px' }}>
              ★ Daily Challenge • 2x Points
            </div>
          )}
          <h2 style={{ marginTop: 0, paddingRight: '80px' }}>{poll.question}</h2>      <p style={{ fontSize: '14px', color: 'var(--text)' }}>Category: {poll.category}</p>
      
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
          <p style={{ color: 'var(--text)', fontStyle: 'italic', margin: 0 }}>You have not voted in this poll.</p>
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

      {isClosed && !poll.isResolved && hasVoted && (
        <div style={{ marginTop: '20px', padding: '12px', textAlign: 'center', color: 'var(--text)', fontStyle: 'italic' }}>
          Voting has ended.
        </div>
      )}

      {poll.isResolved && hasVoted && (
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
            ? `Correct! Your answer won. You have won ${bet * 2}${poll.votes[0]?.isEarlyBird ? ' (1.5x Early Bird!)' : ''} points!` 
            : `Incorrect. Your choice has lost. You have lost your betted points.`}
        </div>
      )}

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