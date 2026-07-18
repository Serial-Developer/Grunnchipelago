@echo off
REM Lance le serveur Archipelago local sur la seed courante.
REM GARDER CETTE FENETRE OUVERTE pendant que tu joues (la fermer = serveur coupe).
REM Commandes utiles dans cette console : /send Grunn1 Speed Trap   (test d'un trap)
cd /d "%~dp0"
set SKIP_REQUIREMENTS_UPDATE=1
Archipelago\.venv313\Scripts\python.exe scripts\serve.py dist\Grunn1_seed6.archipelago 38281
pause
