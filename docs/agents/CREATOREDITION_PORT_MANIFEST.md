# CreatorEdition → SoulCore port manifest (PROP-20)

Source: C:\Users\kurtw\OneDrive\Apps\SoulCore.AI_CreatorEdition
Target: Linearthrone/SoulCore (this repo)

## Core
- SoulCore.Core/Persona/*

## Config
- SoulCore.Config/PersonaOptions.cs
- SoulCore.Config/PersonaMemoryPaths.cs
- SoulCore.Config/PersonaToolPaths.cs

## Host
- SoulCore.Host/Persona/*
- Hosting/ServiceCollectionExtensions/PersonaServiceCollectionExtensions.cs
- MapPersonaApi in WebApplicationExtensions

## Desktop (Phase 3)
- PersonaWizardWindow.*
- Services/PersonaWizardLogic.cs, SoulCorePersonaClient.cs, PersonaQuarantineConfirm.cs
- MainWindow.Persona.cs → adapt into Identity tab

## Tests
- PersonaPackStoreTests, PersonaStoreHubQuarantineTests, PersonaTraitCompilerTests, PersonaToolPathsTests
- House.ChatDesktop.Tests Persona* wizard tests
