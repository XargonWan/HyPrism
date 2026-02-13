import React, { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'framer-motion';
import { User, Shield, ArrowRight, ArrowLeft, Loader2, Dices, Check, AlertCircle, AlertTriangle } from 'lucide-react';
import { useAccentColor } from '../contexts/AccentColorContext';
import { ipc, Profile } from '@/lib/ipc';

type WizardStep = 'choose-type' | 'official-auth' | 'unofficial-name' | 'done';
type ErrorLevel = 'error' | 'warning';

/** Offline nickname regex: 3-13 chars, English letters, digits, dash, underscore */
const NICK_REGEX = /^[a-zA-Z0-9_-]{3,13}$/;

interface ProfileCreationWizardProps {
    onComplete: (profile: Profile) => void;
    onCancel: () => void;
}

/** Short adjectives + nouns for random nick generation (3-13 chars, ASCII only) */
function generateRandomName(): string {
    const adjectives = [
        'Happy', 'Swift', 'Brave', 'Noble', 'Quiet', 'Bold', 'Lucky', 'Epic',
        'Jolly', 'Lunar', 'Solar', 'Azure', 'Royal', 'Foxy', 'Wacky', 'Zesty'
    ];
    const nouns = [
        'Panda', 'Tiger', 'Wolf', 'Fox', 'Bear', 'Eagle', 'Hawk', 'Lion',
        'Raven', 'Owl', 'Shark', 'Cobra', 'Lynx', 'Ace', 'Star', 'Hero'
    ];
    const adj = adjectives[Math.floor(Math.random() * adjectives.length)];
    const noun = nouns[Math.floor(Math.random() * nouns.length)];
    const name = `${adj}${noun}`;
    // Ensure 3-13 chars
    return name.length <= 13 ? name : name.substring(0, 13);
}

function generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

export const ProfileCreationWizard: React.FC<ProfileCreationWizardProps> = ({ onComplete, onCancel }) => {
    const { t } = useTranslation();
    const { accentColor } = useAccentColor();

    const [step, setStep] = useState<WizardStep>('choose-type');
    const [isOfficial, setIsOfficial] = useState(false);
    const [name, setName] = useState(generateRandomName());
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [errorLevel, setErrorLevel] = useState<ErrorLevel>('error');

    // --- Step: Choose Type ---
    const handleChooseOfficial = () => {
        setIsOfficial(true);
        setStep('official-auth');
        setError(null);
    };

    const handleChooseUnofficial = () => {
        setIsOfficial(false);
        setStep('unofficial-name');
        setError(null);
    };

    // --- Step: Official Auth ---
    const handleStartAuth = async () => {
        setIsLoading(true);
        setError(null);
        setErrorLevel('error');
        try {
            // This will open the browser and wait for callback (up to 15 min)
            const result = await ipc.auth.login();
            if (result?.loggedIn && result.username && result.uuid) {
                // Create official profile from Hytale account data
                const profile = await ipc.profile.create({
                    name: result.username,
                    uuid: result.uuid,
                    isOfficial: true,
                });
                if (profile && profile.id) {
                    onComplete(profile);
                } else {
                    setError(t('profiles.wizard.createFailed'));
                    setErrorLevel('error');
                }
            } else if (result?.errorType === 'no_profile') {
                // Yellow warning — account works but has no game profiles
                setError(t('profiles.wizard.noHytaleProfile'));
                setErrorLevel('warning');
            } else {
                setError(t('profiles.wizard.authFailed'));
                setErrorLevel('error');
            }
        } catch (err) {
            console.error('[ProfileWizard] Auth failed:', err);
            setError(t('profiles.wizard.authError'));
            setErrorLevel('error');
        } finally {
            setIsLoading(false);
        }
    };

    // --- Step: Unofficial Name ---
    const isNickValid = useMemo(() => NICK_REGEX.test(name.trim()), [name]);

    const handleCreateUnofficial = async () => {
        const trimmed = name.trim();
        if (!isNickValid) {
            setError(t('profiles.wizard.nickInvalid'));
            setErrorLevel('error');
            return;
        }

        setIsLoading(true);
        setError(null);
        try {
            const uuid = generateUUID();
            const profile = await ipc.profile.create({
                name: trimmed,
                uuid,
                isOfficial: false,
            });
            if (profile && profile.id) {
                onComplete(profile);
            } else {
                setError(t('profiles.wizard.createFailed'));
            }
        } catch (err) {
            console.error('[ProfileWizard] Create failed:', err);
            setError(t('profiles.wizard.createError'));
        } finally {
            setIsLoading(false);
        }
    };

    const handleNameKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter') handleCreateUnofficial();
        if (e.key === 'Escape') onCancel();
    };

    // Shared animation props
    const pageVariants = {
        initial: { opacity: 0, x: 40 },
        animate: { opacity: 1, x: 0 },
        exit: { opacity: 0, x: -40 },
    };

    return (
        <div className="flex flex-col items-center justify-center h-full px-8 py-6">
            <AnimatePresence mode="wait">
                {/* ======== STEP: Choose Profile Type ======== */}
                {step === 'choose-type' && (
                    <motion.div
                        key="choose-type"
                        variants={pageVariants}
                        initial="initial"
                        animate="animate"
                        exit="exit"
                        transition={{ duration: 0.2 }}
                        className="flex flex-col items-center gap-6 max-w-md w-full"
                    >
                        <h2 className="text-xl font-bold text-white">
                            {t('profiles.wizard.title')}
                        </h2>
                        <p className="text-sm text-white/50 text-center">
                            {t('profiles.wizard.chooseType')}
                        </p>

                        <div className="flex flex-col gap-3 w-full">
                            {/* Official */}
                            <button
                                onClick={handleChooseOfficial}
                                className="flex items-center gap-4 p-4 rounded-xl border border-white/[0.08] bg-[#2c2c2e] hover:border-white/20 transition-all group"
                            >
                                <div
                                    className="w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0"
                                    style={{ backgroundColor: `${accentColor}20` }}
                                >
                                    <Shield size={24} style={{ color: accentColor }} />
                                </div>
                                <div className="flex-1 text-left">
                                    <p className="font-semibold text-white group-hover:text-white">
                                        {t('profiles.wizard.official')}
                                    </p>
                                    <p className="text-xs text-white/40 mt-0.5">
                                        {t('profiles.wizard.officialDesc')}
                                    </p>
                                </div>
                                <ArrowRight size={18} className="text-white/20 group-hover:text-white/50 transition-colors" />
                            </button>

                            {/* Unofficial */}
                            <button
                                onClick={handleChooseUnofficial}
                                className="flex items-center gap-4 p-4 rounded-xl border border-white/[0.08] bg-[#2c2c2e] hover:border-white/20 transition-all group"
                            >
                                <div className="w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0 bg-white/5">
                                    <User size={24} className="text-white/50" />
                                </div>
                                <div className="flex-1 text-left">
                                    <p className="font-semibold text-white group-hover:text-white">
                                        {t('profiles.wizard.unofficial')}
                                    </p>
                                    <p className="text-xs text-white/40 mt-0.5">
                                        {t('profiles.wizard.unofficialDesc')}
                                    </p>
                                </div>
                                <ArrowRight size={18} className="text-white/20 group-hover:text-white/50 transition-colors" />
                            </button>
                        </div>

                        <button
                            onClick={onCancel}
                            className="text-sm text-white/30 hover:text-white/60 transition-colors mt-2"
                        >
                            {t('common.cancel')}
                        </button>
                    </motion.div>
                )}

                {/* ======== STEP: Official Auth ======== */}
                {step === 'official-auth' && (
                    <motion.div
                        key="official-auth"
                        variants={pageVariants}
                        initial="initial"
                        animate="animate"
                        exit="exit"
                        transition={{ duration: 0.2 }}
                        className="flex flex-col items-center gap-6 max-w-md w-full"
                    >
                        <div
                            className="w-16 h-16 rounded-full flex items-center justify-center"
                            style={{ backgroundColor: `${accentColor}20` }}
                        >
                            <Shield size={32} style={{ color: accentColor }} />
                        </div>

                        <h2 className="text-xl font-bold text-white">
                            {t('profiles.wizard.authTitle')}
                        </h2>
                        <p className="text-sm text-white/50 text-center">
                            {t('profiles.wizard.authDesc')}
                        </p>

                        {error && (
                            <div className={`flex items-center gap-2 text-sm rounded-lg px-4 py-2 w-full ${
                                errorLevel === 'warning'
                                    ? 'text-yellow-400 bg-yellow-500/10 border border-yellow-500/20'
                                    : 'text-red-400 bg-red-500/10 border border-red-500/20'
                            }`}>
                                {errorLevel === 'warning' ? <AlertTriangle size={16} /> : <AlertCircle size={16} />}
                                {error}
                            </div>
                        )}

                        <motion.button
                            whileHover={{ scale: 1.02 }}
                            whileTap={{ scale: 0.98 }}
                            onClick={handleStartAuth}
                            disabled={isLoading}
                            className="w-full py-3 rounded-xl font-medium text-white flex items-center justify-center gap-2 transition-opacity disabled:opacity-50"
                            style={{ backgroundColor: accentColor }}
                        >
                            {isLoading ? (
                                <>
                                    <Loader2 size={18} className="animate-spin" />
                                    {t('profiles.wizard.waitingAuth')}
                                </>
                            ) : (
                                <>
                                    {t('profiles.wizard.loginHytale')}
                                </>
                            )}
                        </motion.button>

                        {isLoading && (
                            <p className="text-xs text-white/30 text-center">
                                {t('profiles.wizard.browserHint')}
                            </p>
                        )}

                        <button
                            onClick={() => { setStep('choose-type'); setError(null); }}
                            disabled={isLoading}
                            className="flex items-center gap-1 text-sm text-white/30 hover:text-white/60 transition-colors disabled:opacity-30"
                        >
                            <ArrowLeft size={14} />
                            {t('common.back')}
                        </button>
                    </motion.div>
                )}

                {/* ======== STEP: Unofficial Name ======== */}
                {step === 'unofficial-name' && (
                    <motion.div
                        key="unofficial-name"
                        variants={pageVariants}
                        initial="initial"
                        animate="animate"
                        exit="exit"
                        transition={{ duration: 0.2 }}
                        className="flex flex-col items-center gap-6 max-w-md w-full"
                    >
                        <div className="w-16 h-16 rounded-full flex items-center justify-center bg-white/5">
                            <User size={32} className="text-white/50" />
                        </div>

                        <h2 className="text-xl font-bold text-white">
                            {t('profiles.wizard.nameTitle')}
                        </h2>
                        <p className="text-sm text-white/50 text-center">
                            {t('profiles.wizard.nameDesc')}
                        </p>

                        <div className="flex items-center gap-2 w-full">
                            <input
                                type="text"
                                value={name}
                                onChange={(e) => { setName(e.target.value); setError(null); }}
                                onKeyDown={handleNameKeyDown}
                                maxLength={13}
                                autoFocus
                                placeholder={t('profiles.wizard.namePlaceholder')}
                                className="flex-1 bg-[#2c2c2e] text-white text-lg font-semibold px-4 py-3 rounded-xl border outline-none text-center"
                                style={{ borderColor: isNickValid || !name.trim() ? accentColor : '#ef4444' }}
                            />
                            <motion.button
                                whileHover={{ scale: 1.1 }}
                                whileTap={{ scale: 0.9 }}
                                onClick={() => setName(generateRandomName())}
                                className="p-3 rounded-xl bg-white/10 text-white/70 hover:bg-white/20"
                                title={t('profiles.generateName')}
                            >
                                <Dices size={20} />
                            </motion.button>
                        </div>

                        <p className={`text-xs ${isNickValid || !name.trim() ? 'text-white/30' : 'text-red-400/70'}`}>
                            {name.length}/13 {t('profiles.wizard.characters')} · {t('profiles.wizard.nickRules')}
                        </p>

                        {error && (
                            <div className="flex items-center gap-2 text-red-400 text-sm bg-red-500/10 border border-red-500/20 rounded-lg px-4 py-2 w-full">
                                <AlertCircle size={16} />
                                {error}
                            </div>
                        )}

                        <motion.button
                            whileHover={{ scale: 1.02 }}
                            whileTap={{ scale: 0.98 }}
                            onClick={handleCreateUnofficial}
                            disabled={isLoading || !isNickValid}
                            className="w-full py-3 rounded-xl font-medium text-white flex items-center justify-center gap-2 transition-opacity disabled:opacity-50"
                            style={{ backgroundColor: accentColor }}
                        >
                            {isLoading ? (
                                <Loader2 size={18} className="animate-spin" />
                            ) : (
                                <>
                                    <Check size={18} />
                                    {t('profiles.wizard.create')}
                                </>
                            )}
                        </motion.button>

                        <button
                            onClick={() => { setStep('choose-type'); setError(null); }}
                            disabled={isLoading}
                            className="flex items-center gap-1 text-sm text-white/30 hover:text-white/60 transition-colors disabled:opacity-30"
                        >
                            <ArrowLeft size={14} />
                            {t('common.back')}
                        </button>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
};

export default ProfileCreationWizard;
