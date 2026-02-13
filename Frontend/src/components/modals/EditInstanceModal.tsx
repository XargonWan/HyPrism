import React, { useState, useEffect, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { X, Image, Loader2 } from 'lucide-react';
import { useAccentColor } from '../../contexts/AccentColorContext';

import { invoke } from '@/lib/ipc';

interface EditInstanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave?: () => void;
  // Instance data
  instanceId: string;
  initialName: string;
  initialIconUrl?: string;
}

export const EditInstanceModal: React.FC<EditInstanceModalProps> = ({
  isOpen,
  onClose,
  onSave,
  instanceId,
  initialName,
  initialIconUrl,
}) => {
  const { t } = useTranslation();
  const { accentColor, accentTextColor } = useAccentColor();

  // Form state
  const [customName, setCustomName] = useState<string>(initialName);
  const [iconFile, setIconFile] = useState<File | null>(null);
  const [iconPreview, setIconPreview] = useState<string | null>(initialIconUrl || null);
  const [isSaving, setIsSaving] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setCustomName(initialName);
      setIconPreview(initialIconUrl || null);
      setIconFile(null);
      setIsSaving(false);
    }
  }, [isOpen, initialName, initialIconUrl]);

  const handleIconSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setIconFile(file);
      // Create preview URL
      const reader = new FileReader();
      reader.onloadend = () => {
        setIconPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSave = async () => {
    if (isSaving) return;
    setIsSaving(true);

    try {
      // Update name if changed
      if (customName !== initialName) {
        await invoke<boolean>('hyprism:instance:rename', {
          instanceId: instanceId,
          customName: customName?.trim() || null,
        });
      }

      // Handle icon upload if provided
      if (iconFile) {
        try {
          const base64 = await fileToBase64(iconFile);
          await invoke('hyprism:instance:setIcon', {
            instanceId: instanceId,
            iconBase64: base64
          });
        } catch (err) {
          console.warn('Failed to set icon:', err);
        }
      }

      // Notify parent
      onSave?.();
      onClose();
    } catch (err) {
      console.error('Failed to save instance:', err);
      setIsSaving(false);
    }
  };

  const fileToBase64 = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        const base64 = (reader.result as string).split(',')[1];
        resolve(base64);
      };
      reader.onerror = reject;
      reader.readAsDataURL(file);
    });
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        className="fixed inset-0 z-[200] flex items-center justify-center bg-[#0a0a0a]/90"
        onClick={(e) => e.target === e.currentTarget && onClose()}
      >
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className="w-full max-w-md mx-4 glass-panel-static-solid shadow-2xl"
        >
          {/* Header */}
          <div className="flex items-center justify-between p-5 border-b border-white/[0.06]">
            <div className="flex items-center gap-3">
              <div>
                <h2 className="text-lg font-bold text-white">{t('instances.editInstance')}</h2>
                <p className="text-xs text-white/40">{t('instances.editInstanceHint')}</p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-2 rounded-xl text-white/40 hover:text-white hover:bg-white/10 transition-colors"
            >
              <X size={20} />
            </button>
          </div>

          {/* Content */}
          <div className="p-4 space-y-3">
            {/* Instance Name + Icon row */}
            <div className="flex items-end gap-3">
              {/* Icon Preview - compact */}
              <div className="flex flex-col items-center gap-1 flex-shrink-0">
                <div 
                  className="w-14 h-10 rounded-xl border-2 border-dashed flex items-center justify-center overflow-hidden cursor-pointer hover:border-white/40 transition-colors"
                  style={{ borderColor: iconPreview ? accentColor : 'rgba(255,255,255,0.2)' }}
                  onClick={() => fileInputRef.current?.click()}
                >
                  {iconPreview ? (
                    <img src={iconPreview} alt="Instance icon" className="w-full h-full object-cover" />
                  ) : (
                    <Image size={18} className="text-white/30" />
                  )}
                </div>
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                className="hidden"
                onChange={handleIconSelect}
              />
              {/* Instance Name */}
              <div className="flex-1 space-y-1">
                <label className="text-xs text-white/50">{t('instances.instanceName')}</label>
                <input
                  type="text"
                  value={customName}
                  onChange={(e) => setCustomName(e.target.value)}
                  placeholder={t('instances.instanceNamePlaceholder')}
                  className="w-full h-10 px-3 rounded-xl bg-[#2c2c2e] border border-white/[0.06] text-white text-sm focus:outline-none focus:border-white/20 transition-colors"
                  maxLength={32}
                />
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="flex justify-end gap-2 p-4 pt-0">
            <button
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl text-sm text-white/60 hover:text-white hover:bg-white/10 transition-colors"
            >
              {t('common.cancel')}
            </button>
            <button
              onClick={handleSave}
              disabled={isSaving}
              className="px-5 py-2.5 rounded-xl text-sm font-semibold transition-all hover:opacity-90 flex items-center gap-2"
              style={{ backgroundColor: accentColor, color: accentTextColor }}
            >
              {isSaving && <Loader2 size={14} className="animate-spin" />}
              {t('common.save')}
            </button>
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
};
