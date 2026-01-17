import React, { useState, useRef, useEffect } from 'react';
import { ChevronDown, Check } from 'lucide-react';

export interface DropdownOption {
  value: string | number;
  label: string;
  icon?: React.ReactNode;
  disabled?: boolean;
}

interface DropdownProps {
  options: DropdownOption[];
  value: string | number;
  onChange: (value: string | number) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  menuClassName?: string;
  icon?: React.ReactNode;
  loading?: boolean;
  loadingText?: string;
  openUp?: boolean;
}

export const Dropdown: React.FC<DropdownProps> = ({
  options,
  value,
  onChange,
  placeholder = 'Select...',
  disabled = false,
  className = '',
  menuClassName = '',
  icon,
  loading = false,
  loadingText = '...',
  openUp = false,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find((opt) => opt.value === value);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Close on escape
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, []);

  const handleSelect = (option: DropdownOption) => {
    if (option.disabled) return;
    onChange(option.value);
    setIsOpen(false);
  };

  return (
    <div ref={dropdownRef} className={`relative ${className}`}>
      {/* Trigger Button */}
      <button
        onClick={() => !disabled && !loading && setIsOpen(!isOpen)}
        disabled={disabled || loading}
        className={`
          h-10 px-3 pr-8 rounded-xl 
          bg-black/40 backdrop-blur-sm
          border border-white/10
          text-sm font-medium
          flex items-center gap-2
          cursor-pointer
          hover:border-white/20 hover:bg-white/5
          disabled:opacity-50 disabled:cursor-not-allowed
          focus:outline-none focus:border-[#FFA845]/50
          transition-colors duration-100
          ${isOpen ? 'border-[#FFA845]/50 bg-white/5' : ''}
        `}
      >
        {icon && <span className="text-white/40">{icon}</span>}
        <span className={selectedOption ? 'text-white/80' : 'text-white/40'}>
          {loading ? loadingText : selectedOption?.label || placeholder}
        </span>
        <ChevronDown 
          size={14} 
          className={`
            absolute right-2 top-1/2 -translate-y-1/2 
            text-white/40 
            transition-transform duration-150
            ${isOpen ? (openUp ? '' : 'rotate-180') : (openUp ? 'rotate-180' : '')}
          `}
        />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div 
          className={`
            absolute ${openUp ? 'bottom-full mb-1' : 'top-full mt-1'} left-0 z-50
            min-w-full w-max max-h-60
            bg-[#1a1a1a] backdrop-blur-xl
            border border-white/10
            rounded-xl
            shadow-xl shadow-black/50
            overflow-hidden
            ${menuClassName}
          `}
        >
          <div className="overflow-y-auto max-h-60 py-1 scrollbar-thin">
            {options.map((option) => (
              <button
                key={option.value}
                onClick={() => handleSelect(option)}
                disabled={option.disabled}
                className={`
                  w-full px-3 py-2
                  flex items-center gap-2
                  text-sm text-left
                  transition-colors duration-75
                  ${option.value === value 
                    ? 'bg-[#FFA845]/20 text-[#FFA845]' 
                    : 'text-white/70 hover:bg-white/10 hover:text-white'
                  }
                  ${option.disabled ? 'opacity-40 cursor-not-allowed' : 'cursor-pointer'}
                `}
              >
                {option.icon && <span className="flex-shrink-0">{option.icon}</span>}
                <span className="flex-1">{option.label}</span>
                {option.value === value && (
                  <Check size={14} className="text-[#FFA845] flex-shrink-0" />
                )}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Custom scrollbar styles */}
      <style>{`
        .scrollbar-thin::-webkit-scrollbar {
          width: 4px;
        }
        .scrollbar-thin::-webkit-scrollbar-track {
          background: rgba(255,255,255,0.05);
        }
        .scrollbar-thin::-webkit-scrollbar-thumb {
          background: rgba(255,255,255,0.2);
          border-radius: 2px;
        }
        .scrollbar-thin::-webkit-scrollbar-thumb:hover {
          background: rgba(255,255,255,0.3);
        }
      `}</style>
    </div>
  );
};

export default Dropdown;
