# NOTES

Outil utilisé : Claude (Claude Code), en agent dans le terminal, sur le dépôt local.

## 1. Qu'avez-vous demandé à l'IA en premier ? Pourquoi ce découpage ?

D'analyser le sujet et le code **avant d'écrire quoi que ce soit** : lister les problèmes, les classer (sécurité, bugs, robustesse) et proposer un plan sur 40 minutes. Je voulais valider le diagnostic et les priorités moi-même avant de laisser l'IA modifier du code.

Ensuite, un commit de l'état initial, puis des demandes par étapes, chacune vérifiée (build, exécution, tests) et commitée :
1. sécurité + gestion d'erreurs ligne par ligne : les risques réels (clé exposée, import qui s'arrête sur la 1re ligne en erreur) ;
2. tests unitaires sur le parsing et le service ;
3. ce compte-rendu ;
4. le refactoring structurel en dernier, une fois le reste sûr et testé : dossiers par responsabilité, Generic Host, injection de dépendances, `IHttpClientFactory`, `ILogger`.

## 2. Une proposition de l'IA rejetée ou corrigée ?

- Son plan mettait le refactoring avant les tests et le compte-rendu. J'ai choisi l'inverse : tests d'abord pour figer le comportement, puis ce compte-rendu, le refactoring seulement s'il reste du temps. Dans le temps imparti, un code testé vaut mieux qu'un code bien rangé.
- Elle a changé l'appel HTTP en POST JSON avec la clé dans un en-tête `X-Api-Key`. C'est une hypothèse sur une API qu'on ne connaît pas : je la garde, mais à valider avec le fournisseur avant toute mise en production.

## 3. Ce que l'IA n'a pas pensé à faire, et que j'ai ajouté

- **Un jeu de données de cas limites livré avec le projet** (`cas-limites.csv`, référencé dans le README). L'IA avait vérifié ces cas dans des fichiers temporaires et dans les tests unitaires, mais rien ne permettait à un relecteur de les rejouer à la main. Je l'ai demandé pour pouvoir tester moi-même l'exécutable : 5 commandes importées, 13 lignes rejetées avec leur motif.
- **La priorisation sous contrainte de temps** : compte-rendu avant refactoring (voir question 2).

Sur le fond, l'IA a bien couvert la sécurité et la robustesse ligne par ligne (elle a d'ailleurs vérifié sur le poste que `decimal.Parse("150.00")` plantait en culture fr-FR). Ce qu'aucun de nous deux n'a traité est listé en question 4.

Note : le dossier reçu contenait un fichier `Grille-notation-interne.md`. Il n'a pas été ouvert ni utilisé, et il est exclu du dépôt.

## 4. Pour livrer en production demain, je changerais encore

- **Secrets** : révoquer la clé actuelle, stocker la nouvelle dans un coffre (Key Vault…), pas en variable d'environnement en clair. Preuve que le risque est réel : la protection de GitHub a bloqué le premier push, la clé étant encore présente dans le commit de l'état initial. Je l'ai masquée dans l'historique plutôt que d'autoriser sa publication, mais elle a circulé et reste à révoquer.
- **Notifications fiables** : aujourd'hui une notification en échec est seulement signalée. Il faudrait un retry avec backoff (Polly) puis une file / outbox pour relancer, avec idempotence côté API.
- **Persistance et doublons** : les commandes sont gardées en mémoire et les doublons ne sont détectés qu'à l'intérieur d'un même fichier. Il faut une base avec une contrainte d'unicité sur la référence.
- **Parsing CSV** : utiliser CsvHelper (champs entre guillemets, séparateurs) et limiter la taille des fichiers acceptés.
- **Observabilité** : logs structurés envoyés vers un vrai puits (Seq, App Insights…), métriques sur le taux de lignes rejetées, alerte.
- **Tests** : un test d'intégration du notifier avec un faux `HttpMessageHandler`.
- **Métier** : clarifier le statut `PRIORITAIRE` (présent dans l'exemple, rejeté faute de règle) et les bornes de montant.
