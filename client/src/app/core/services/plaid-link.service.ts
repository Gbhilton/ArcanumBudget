import { Injectable } from '@angular/core';

// Loaded globally via the <script> tag in index.html (Plaid has no official
// Angular package — the vanilla Link SDK + a global `Plaid` object is the
// standard integration for non-React apps).
interface PlaidLinkMetadata {
  institution?: { name: string; institution_id: string } | null;
}

interface PlaidLinkHandler {
  open(): void;
  destroy(): void;
}

interface PlaidLinkOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: PlaidLinkMetadata) => void;
  onExit?: () => void;
}

declare const Plaid: {
  create(options: PlaidLinkOptions): PlaidLinkHandler;
};

@Injectable({ providedIn: 'root' })
export class PlaidLinkService {
  open(
    linkToken: string,
    onSuccess: (publicToken: string, institutionName: string) => void,
    onExit?: () => void,
  ): void {
    const handler = Plaid.create({
      token: linkToken,
      onSuccess: (publicToken, metadata) => {
        onSuccess(publicToken, metadata.institution?.name ?? 'Unknown institution');
      },
      onExit: () => onExit?.(),
    });
    handler.open();
  }
}
