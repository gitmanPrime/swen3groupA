import { Injectable } from '@angular/core';
import Keycloak from 'keycloak-js';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly keycloak = new Keycloak({
    url: 'http://localhost:8082',
    realm: 'dms',
    clientId: 'dms-frontend'
  });

  init(): Promise<boolean> {
    return this.keycloak.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256'
    });
  }

  get isLoggedIn(): boolean {
    return this.keycloak.authenticated === true;
  }

  login(): Promise<void> {
    return this.keycloak.login({
      redirectUri: `${window.location.origin}/documents`
    });
  }

  logout(): Promise<void> {
    return this.keycloak.logout({
      redirectUri: window.location.origin
    });
  }
}
