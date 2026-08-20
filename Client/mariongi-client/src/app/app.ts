import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ecouteinactivite } from './services/ecouteinactivite.services'; // Assurez-vous du nom exact de la classe et du fichier

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.scss'],
  imports: [RouterOutlet],
})
export class App implements OnInit {
  
  // On injecte le service dans une variable privée (ex: _inactiviteService)
  constructor(private _inactiviteService: ecouteinactivite) {}

  ngOnInit(): void {
    // Démarre l'écoute de l'inactivité dès le chargement de l'app
    this._inactiviteService.startWatching();
  }
}