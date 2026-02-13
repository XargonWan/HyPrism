import React, { useState, useEffect, useRef, useCallback, memo } from 'react';
import { Volume2, VolumeX } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAccentColor } from '../../contexts/AccentColorContext';

// Import music tracks
import menu01 from '../../assets/music/menu_01.ogg';
import menu02 from '../../assets/music/menu_02.ogg';
import menu03 from '../../assets/music/menu_03.ogg';
import menu04 from '../../assets/music/menu_04.ogg';
import menu05 from '../../assets/music/menu_05.ogg';
import menu06 from '../../assets/music/menu_06.ogg';
import menu07 from '../../assets/music/menu_07.ogg';
import menu08 from '../../assets/music/menu_08.ogg';
import menu09 from '../../assets/music/menu_09.ogg';
import menu10 from '../../assets/music/menu_10.ogg';

const musicTracks = [
  menu01, menu02, menu03, menu04, menu05,
  menu06, menu07, menu08, menu09, menu10
];

import { ipc } from '@/lib/ipc';

interface MusicPlayerProps {
  className?: string; // Kept for compatibility but unused
  muted?: boolean;
  forceMuted?: boolean;
}

export const MusicPlayer: React.FC<MusicPlayerProps> = memo(({ className = '', muted = false, forceMuted = false }) => {
  const { t } = useTranslation();
  // Internal isMuted state removed - controlled by prop
  const [currentTrack, setCurrentTrack] = useState(() => 
    Math.floor(Math.random() * musicTracks.length)
  );
  const [isFading, setIsFading] = useState(false);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const fadeIntervalRef = useRef<number | null>(null);
  const targetVolumeRef = useRef(0.3);

  // Get a random track different from the current one
  const getNextRandomTrack = useCallback((current: number) => {
    if (musicTracks.length <= 1) return 0;
    let next = current;
    while (next === current) {
      next = Math.floor(Math.random() * musicTracks.length);
    }
    return next;
  }, []);

  // Handle forceMuted or muted prop change with smooth fade
  useEffect(() => {
    if (!audioRef.current) return;

    const shouldBeSilent = muted || forceMuted;

    if (fadeIntervalRef.current) {
      clearInterval(fadeIntervalRef.current);
      fadeIntervalRef.current = null;
    }

    if (shouldBeSilent && !isFading) {
      setIsFading(true);
      const startVolume = audioRef.current.volume;
      const steps = 20;
      const stepTime = 1000 / steps;
      const volumeStep = startVolume / steps;
      let currentStep = 0;

      fadeIntervalRef.current = window.setInterval(() => {
        currentStep++;
        if (audioRef.current) {
          audioRef.current.volume = Math.max(0, startVolume - (volumeStep * currentStep));
        }
        if (currentStep >= steps) {
          if (fadeIntervalRef.current) clearInterval(fadeIntervalRef.current);
          if (audioRef.current) {
            audioRef.current.pause();
            audioRef.current.currentTime = 0;
          }
          setIsFading(false);
        }
      }, stepTime);
    } else if (!shouldBeSilent && audioRef.current.paused) {
      setIsFading(true);
      const targetVolume = targetVolumeRef.current;
      audioRef.current.volume = 0;

      audioRef.current.play().catch(err => {
        console.log('Failed to resume audio:', err);
        setIsFading(false);
        return;
      });

      const steps = 20;
      const stepTime = 1000 / steps;
      const volumeStep = targetVolume / steps;
      let currentStep = 0;

      fadeIntervalRef.current = window.setInterval(() => {
        currentStep++;
        if (audioRef.current) {
          audioRef.current.volume = Math.min(targetVolume, volumeStep * currentStep);
        }
        if (currentStep >= steps) {
          if (fadeIntervalRef.current) clearInterval(fadeIntervalRef.current);
          setIsFading(false);
        }
      }, stepTime);
    }

    return () => {
      if (fadeIntervalRef.current) clearInterval(fadeIntervalRef.current);
    };
  }, [forceMuted, muted]);

  // Handle track ending - play next random track
  const handleEnded = useCallback(() => {
    const nextTrack = getNextRandomTrack(currentTrack);
    setCurrentTrack(nextTrack);
  }, [currentTrack, getNextRandomTrack]);

  // Play audio when track changes
  useEffect(() => {
    if (!audioRef.current) return;

    const audio = audioRef.current;
    audio.volume = targetVolumeRef.current;

    // Play if not muted and not force muted
    if (!muted && !forceMuted) {
      audio.play().catch(err => {
        console.log('Auto-play blocked:', err);
        const handleUserInteraction = async () => {
            try { await audio.play(); } catch {}
            document.removeEventListener('click', handleUserInteraction);
            document.removeEventListener('keydown', handleUserInteraction);
        };
        document.addEventListener('click', handleUserInteraction);
        document.addEventListener('keydown', handleUserInteraction);
      });
    }
  }, [currentTrack, muted, forceMuted]);

  return (
    <audio
      ref={audioRef}
      src={musicTracks[currentTrack]}
      onEnded={handleEnded}
      preload="auto"
    />
  );
});

MusicPlayer.displayName = 'MusicPlayer';
