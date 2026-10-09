import { useState, useEffect } from 'react';

export function TimerAndProgress({ createdAt, closingTime }) {
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