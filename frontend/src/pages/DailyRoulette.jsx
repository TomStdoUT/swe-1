import { useEffect, useRef, useState } from 'react';
import './DailyRoulette.css';

const EVEN_SLICE_COLORS = [
  'rgba(170, 59, 255, 0.14)',
  'rgba(170, 59, 255, 0.28)',
];

const ODD_SLICE_COLORS = [
  'rgba(170, 59, 255, 0.12)',
  'rgba(170, 59, 255, 0.24)',
  'rgba(170, 59, 255, 0.36)',
];

function buildWheelBackground(segmentCount) {
  if (segmentCount <= 0) return EVEN_SLICE_COLORS[0];

  const sliceAngle = 360 / segmentCount;
  const isOdd = segmentCount % 2 === 1;
  const stops = [];

  for (let i = 0; i < segmentCount; i++) {
    let color;
    if (!isOdd) {
      color = EVEN_SLICE_COLORS[i % 2];
    } else if (i === segmentCount - 1 && i % 3 === 0) {
      color = ODD_SLICE_COLORS[1];
    } else {
      color = ODD_SLICE_COLORS[i % 3];
    }

    const startDeg = i * sliceAngle;
    const endDeg = (i + 1) * sliceAngle;
    stops.push(`${color} ${startDeg}deg ${endDeg}deg`);
  }

  return `conic-gradient(${stops.join(', ')})`;
}

export function DailyRoulette({ onBack, onSpinSuccess }) {
  const [status, setStatus] = useState(null);
  const [rotationDeg, setRotationDeg] = useState(0);
  const [isSpinning, setIsSpinning] = useState(false);
  const [resultBanner, setResultBanner] = useState(null);
  const [error, setError] = useState('');
  const [countdownText, setCountdownText] = useState('');
  const spinTimeoutRef = useRef(null);

  useEffect(() => {
    return () => {
      if (spinTimeoutRef.current !== null) {
        clearTimeout(spinTimeoutRef.current);
      }
    };
  }, []);

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
  const wheelBackground = buildWheelBackground(segments.length);
  const minPoints = segments.length > 0 ? Math.min(...segments) : null;
  const maxPoints = segments.length > 0 ? Math.max(...segments) : null;

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

      if (spinTimeoutRef.current !== null) {
        clearTimeout(spinTimeoutRef.current);
      }

      spinTimeoutRef.current = setTimeout(() => {
        spinTimeoutRef.current = null;
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
          <p>
            {minPoints !== null && maxPoints !== null
              ? `One spin per day. Win between ${minPoints} and ${maxPoints} points.`
              : 'One spin per day.'}
          </p>
        </div>
        <button type="button" onClick={onBack}>Back</button>
      </div>

      {error && <p className="roulette-error" role="alert">{error}</p>}

      {status && (
        <section className="roulette-stage">
          <div className="roulette-pointer" aria-hidden="true" />
          <div
            className="roulette-wheel"
            style={{
              transform: `rotate(${rotationDeg}deg)`,
              background: wheelBackground,
            }}
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