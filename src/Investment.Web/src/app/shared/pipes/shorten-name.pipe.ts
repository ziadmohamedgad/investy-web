import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'shortenName',
  standalone: true
})
export class ShortenNamePipe implements PipeTransform {
  // Common prepositions and conjunctions to remove if they appear at the end after truncation
  private stopWords = [
    'and', '&', 'of', 'in', 'for', 'to', 'at', 'on', 'the', 'a', 'an', 'or', 'with', 'by',
    'و', 'في', 'من', 'إلى', 'على', 'عن', 'مع', 'لـ', 'بـ', 'كـ', 'أو', 'ثم'
  ];

  transform(value: string | undefined | null, maxLength: number = 25): string {
    if (!value) return '';
    
    value = value.trim();
    if (value.length <= maxLength) return value;

    // Truncate to max length
    let truncated = value.substring(0, maxLength);
    
    // Find the last space to avoid cutting a word in half
    const lastSpaceIndex = truncated.lastIndexOf(' ');
    if (lastSpaceIndex > 0) {
      truncated = truncated.substring(0, lastSpaceIndex);
    }

    // Clean up trailing prepositions/stop words
    let words = truncated.split(/\s+/);
    while (words.length > 0) {
      const lastWord = words[words.length - 1].toLowerCase().replace(/[^a-z0-9&أ-ي]/g, '');
      if (this.stopWords.includes(lastWord) || lastWord === '') {
        words.pop();
      } else {
        break;
      }
    }

    // Rejoin words
    truncated = words.join(' ');
    
    // If we stripped everything somehow, just fallback to hard truncation
    if (!truncated) {
      return value.substring(0, maxLength);
    }

    return truncated;
  }
}
