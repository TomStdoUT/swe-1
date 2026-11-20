import { useEffect, useState } from 'react';
import './DailyRoulette.css';

export function DailyRoulette({ onBack, onSpinSuccess }) {
  const [status, setStatus] = useState(null);
  const [rotationDeg, setRotationDeg] = useState(0);
  const [isSpinning, setIsSpinning] = useState(false);
  const [resultBanner, setResultBanner] = useState(null);
  const [error, setError] = useState('');
  const [countdownText, setCountdownText] = useState('');

  useEffect(() => {
    let active = true;

    const loadStatus = async () => {
      try {
        const res = await fetch('/api/roulette/status', {
          headers: { Authorization: `Bearer ${sessionStorage.getItem('poll-token')}` },
        });
        if (!res.ok) throw new Error('Failed to load roulette status.');
        const data = await res.json();
        if (active) {
          setStatus(data);
          setError('');
        }
      } catch {
        if (active) setError('Could not load daily roulette status.');
      }
    };

    loadStatus();
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!status?.nextSpinAvailableAtUtc || status.canSpin) {
      setCountdownText('');
      return;
    }

    const updateTimer = () => {
      const remainingMs = new Date(status.nextSpinAvailableAtUtc).getTime() - Date.now();
      if (remainingMs <= 0) {
        setCountdownText('');
        setStatus((prev) => (prev ? { ...prev, canSpin: true, nextSpinAvailableAtUtc: null } : prev));
        return;
      }
      const totalSec = Math.floor(remainingMs / 1000);
      const hrs = Math.floor(totalSec / 3600);
      const mins = Math.floor((totalSec % 3600) / 60);
      const secs = totalSec % 60;
      setCountdownText(`${hrs}h ${mins}m ${secs}s`);
    };

    updateTimer();
    const timer = setInterval(updateTimer, 1000);
    return () => clearInterval(timer);
  }, [status]);

  const segments = status?.wheelSegments ?? [];
  const segmentAngle = segments.length > 0 ? 360 / segments.length : 0;

  const handleSpin = async () => {
    if (!status?.canSpin || isSpinning || segments.length === 0) return;

    setError('');
    setResultBanner(null);
    setIsSpinning(true);

    try {
      const res = await fetch('/api/roulette/spin', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${sessionStorage.getItem('poll-token')}`,
        },
      });

      const data = await res.json().catch(() => ({}));
      if (!res.ok) {
        throw new Error(data.error || 'Spin failed.');
      }

      const targetCenter = data.winningSegmentIndex * segmentAngle + segmentAngle / 2;
      const fullSpins = 5 * 360;
      const currentMod = ((rotationDeg % 360) + 360) % 360;
      const desiredMod = ((360 - targetCenter) % 360 + 360) % 360;
      const delta = (desiredMod - currentMod + 360) % 360;
      const nextRotation = rotationDeg + fullSpins + delta;

      setRotationDeg(nextRotation);

      setTimeout(() => {
        setIsSpinning(false);
        setResultBanner(`+${data.awardedPoints} pts`);
        setStatus((prev) => ({
          ...(prev || {}),
          canSpin: false,
          nextSpinAvailableAtUtc: data.nextSpinAvailableAtUtc,
          lastAwardedPoints: data.awardedPoints,
          wheelSegments: segments,
        }));
        if (onSpinSuccess) {
          onSpinSuccess(data.newPointsBalance);
        }
      }, 3200);
    } catch (err) {
      setIsSpinning(false);
      setError(err.message);
    }
  };

  return (
    <main className="roulette-page">
      <div className="roulette-header">
        <div>
          <h1>Daily Roulette</h1>
          <p>One spin per day. Win between 10 and 100 points.</p>
        </div>
        <button type="button" onClick={onBack}>Back</button>
      </div>

      {error && <p className="roulette-error" role="alert">{error}</p>}

      {status && (
        <section className="roulette-stage">
          <div className="roulette-pointer" aria-hidden="true" />
          <div
            className="roulette-wheel"
            style={{ transform: `rotate(${rotationDeg}deg)` }}
          >
            {segments.map((pts, idx) => {
              const angle = idx * segmentAngle;
              return (
                <div
                  key={`${pts}-${idx}`}
                  className="roulette-label"
                  style={{
                    transform: `rotate(${angle + segmentAngle / 2}deg) translateY(-105px)`,
                  }}
                >
                  <span>{pts}</span>
                </div>
              );
            })}
          </div>

          <div className="roulette-actions">
            {status.canSpin ? (
              <button
                type="button"
                className="roulette-spin-btn"
                disabled={isSpinning}
                onClick={handleSpin}
              >
                {isSpinning ? 'Spinning...' : 'Spin Daily Roulette'}
              </button>
            ) : (
              <p className="roulette-cooldown">
                Next spin available in: <strong>{countdownText || 'Calculating...'}</strong>
              </p>
            )}

            {resultBanner && (
              <div className="roulette-win-banner">
                Won <strong>{resultBanner}</strong>!
              </div>
            )}

            {!resultBanner && status.lastAwardedPoints != null && (
              <p className="roulette-last">
                Last spin reward: <strong>+{status.lastAwardedPoints} pts</strong>
              </p>
            )}
          </div>
        </section>
      )}
    </main>
  );
}