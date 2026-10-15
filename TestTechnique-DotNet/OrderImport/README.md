# OrderImport

Import de commandes fournisseurs depuis un fichier CSV (.NET 8).

## Lancer

```bash
dotnet run                                   # importe sample-orders.csv
dotnet run -- chemin/vers/mon-fichier.csv
dotnet run -- cas-limites.csv                # doublons, montants, statuts, colonnes, identifiants invalides
```

La clé de l'API de notification est lue dans la variable d'environnement `NOTIF_API_KEY`. Sans elle, l'import fonctionne mais les commandes URGENT ne sont pas notifiées.

Codes de sortie : `0` tout est importé, `2` import partiel (lignes rejetées ou notifications en échec), `1` fichier introuvable, en-tête invalide ou import annulé.

## Tests

```bash
dotnet test OrderImport.sln
```

## Choix d'architecture

- `OrderLineParser` : transforme une ligne en `Order` ou en motif de rejet. Aucune exception pour les cas attendus, validation stricte des identifiants (ils partent vers l'API et dans les logs), montant en culture invariante.
- `OrderImportService` : lit le fichier en flux, ligne par ligne. Une ligne invalide, en double ou qui provoque une erreur imprévue est rejetée avec son numéro, le reste continue. Retourne un `ImportResult` au lieu d'écrire dans une liste statique.
- `ISupplierNotifier` : abstraction de la notification, ce qui permet de tester le service sans appel réseau. `HttpSupplierNotifier` envoie la clé en en-tête et les données en JSON. Un échec de notification n'annule pas l'import de la commande, il est remonté dans le résultat.
- `Program` : assemble le tout, lit la configuration et affiche le récapitulatif.
