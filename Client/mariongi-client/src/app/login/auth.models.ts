/**
 * Étape courante du flux de connexion à deux facteurs.
 */
export type LoginStep = 'credentials' | 'otp';

/**
 * Payload envoyé à POST /api/Auth/connexion
 */
export interface LoginPayload {
  identifiant: string; // téléphone ou e-mail
  motDePasse: string;
}

/**
 * Payload envoyé à POST /api/Auth/valider-otp
 */
export interface VerifyOtpPayload {
  Telephone: string;
  CodeOtp: string;
}

/**
 * Réponse commune aux deux endpoints /connexion et /valider-otp.
 * - Après /connexion : si requisOtp = true, les tokens sont null et
 *   `telephone` indique le numéro sur lequel le SMS a été envoyé.
 * - Après /valider-otp réussi : requisOtp = false, les tokens sont renseignés.
 */
export interface AuthResponse {
  requisOtp: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  expiration: string | null;
  telephone: string | null;
  /**
   * ⚠️ DEV UNIQUEMENT : présent tant qu'il n'y a pas de vraie passerelle SMS
   * côté backend. À retirer de l'API dès que l'envoi SMS réel est en place —
   * ne doit JAMAIS être renvoyé par le serveur en production.
   */
  codeParSms?: string | null;
}

/**
 * Forme normalisée d'une erreur API.
 */
export interface ApiError {
  status: number;
  code?: string;
  message: string;
  retryAfterSeconds?: number; // utile pour le rate limiting (HTTP 429)
}