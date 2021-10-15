# Bouvet Workshop: MXChip IoT DevKit AZ3166 Oversetter

I denne workshopen skal vi lære hvordan vi kan bruke en MXChip IoT DevKit AZ3166 som en språk oversetter ved hjelp av Azure Cognitive Services. Etter workshopen skal devicen kunne ta opp stemmen din, oversette det du sa i skyen, og skrive det ut til skjermen på kortet.

Måten systemet fungerer på er at MXChipen tar opp stemmen din og poster en HTTP request for å trigge en Azure Function. Azure Functionen kaller på Azure Cognitive Services tale oversettelses API for å gjøre oversettelsen. Etter at Azure Funksjonen får returnert oversatt teks sender den en C2D (Cloud-to-Device) melding til devicen. Den oversatte teksen blir så vist på skjermen.

<img src="diagram.png" alt="arkitektur" width=600>

### Disclaimer!

Denne workshopen baserer seg i stor grad på denne artikkelen [her](https://docs.microsoft.com/en-us/samples/azure-samples/mxchip-iot-devkit-translator/sample/). Bouvet tar ikke eierskap til koden, men har gjort endringer for å tilpasse den denne workshopen.

## Prerequisites

Dette trenger du for å kunne gjennomføre workshopen.

- En MXChip IoT DevKit Board. (Dette får du utdelt på kurset).
- En PC som kjører Windows 10, macOS 10.10+ eller Ubuntu 18.04+.
- En aktiv Azure Subscription. [Du kan aktivere en 30-dagers trial lisens her.](https://azure.microsoft.com/nb-no/free/)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Arduino IDE](https://www.arduino.cc/en/software) (OBS! For Windows brukere: Benytt Windows Installer, ikke App Store versjonen.)
- ST-Link Drivere

  - Windows: Last ned og installer [her.](https://www.st.com/en/development-tools/stsw-link009.html)
  - macOS: Driver er ikke nødvendig for macOS.
  - Kjør følgende kommando i terminalen, deretter logg ut og logg inn for at endringen skal ta effekt.

  ```
  # Copy the default rules. This grants permission to the group 'plugdev'
  sudo cp ~/.arduino15/packages/AZ3166/tools/openocd/0.10.0/linux/contrib/60-openocd.rules /etc/udev/rules.d/
  sudo udevadm control --reload-rules

  # Add yourself to the group 'plugdev'
  # Logout and log back in for the group to take effect
  sudo usermod -a -G plugdev $(whoami)'
  ```

### Gjør klar utviklingsmiljøet

Etter å ha installert det overnevnte er vi klare til å sette opp utviklingsmiljøet.

1.  Start VSCode, og klikk på _Extensions_ (Eller `Ctrl + Shift + X`). Søk opp **Arduino** og installer.
2.  Søk også opp **Azure IoT Tools** inne på _Extensions_ og installer.
3.  Konfigurer VSCode med Arduino Instillinger.

    - Klikk på Fil > Preferences > Settings.
    - Klikk så på _Open Settings (JSON)_
    - Legg til følgende linjer avhenging av ditt OS.

      - **Windows:**
        ```
        "arduino.path": "C:\\Program Files (x86)\\Arduino",
        "arduino.additionalUrls":"https://raw.githubusercontent.com/VSChina/azureiotdevkit_tools/master/package_azureboard_index.json"
        ```
      - **macOS:**
        ```
        "arduino.path": "/Applications",
        "arduino.additionalUrls":"https://raw.githubusercontent.com/VSChina/azureiotdevkit_tools/master/package_azureboard_index.json"
        ```
      - **Ubuntu:**

        _Bytt ut **{username}** med ditt brukernavn._

        ```
        "arduino.path": "/home/{username}/Downloads/arduino-1.8.8",
        "arduino.additionalUrls":"https://raw.githubusercontent.com/VSChina/azureiotdevkit_tools/master/package_azureboard_index.json"
        ```

4.  Click `F1` for å åpne _Command Palette_. Skriv og velg **Arduino: Board Manager**. Søk på AZ3166 og installer den nyeste versjonen.

## Provisjoner Azure IoTHub og DevKit

Det følger med en haug med gode eksempel-prosjekter med MXChipen, derfor er det en god device å starte med når man skal lære om IoT. Vi skal ta utgangspunkt i et av disse eksemplene.

1. Start med å koble MXChipen fra PC'en. Start VSCode først, og deretter koble MXChipen til.

2. Klikk `F1` for å åpne _Command Palette_. Søk og velg **Azure IoT Device Workbench: Open Example...** Velg så **IoT DevKit** som board.

3. På _IoT Workbench Examples_ siden, finn **DevKit Translator** og klikk **Open Sample**.

4. Klikk nå på `F1`, søk og velg **Azure IoT Device Workbench: Provision Azure Services...** Følg så prossessen.

5. Velg korrekt subscription.

6. Velg _Create Resource Group_. Følg guiden og lage en ny ressursgruppe.

7. Velg så _Create new IoTHub_. Følg guiden, du skal nå kunne se en provisjonert IoT Hub i output vinduet.

8. Velg så _Create a new IoT Hub device_, og velg AZ3166.

NB! Kopier og noter ned _Connection String_, denne trenger du senere.

## Koble til MXChip til WiFi

Vi skal nå gjøre MXChipen klar.

1. Last ned nyeste versjonen av firmware for IoT DevKitet [her.](https://aka.ms/devkit/prod/getstarted/latest)

2. Åpne **AZ3166** inne i filsystemet på PC'en.

3. Dra og slipp firmware filen her.

4. På MXChipen, hold ned knapp **B** og klikk en gang på **Reset** knappen. Slipp så **B** knappen. MXChipen går inn i i _AP mode_. For å bekrefte dette kan du se på skjermen at MXChipens SSID og IP-adressen for konfigurasjon vises.

NB! Her er det lurt å kopiere inn _Connection String_-en fra tidligere.

5. Åpne WiFi innstillinger og koble deg til WiFiet som tilsvarer SSID på din MXChip. Hvis den ber om et passord, la det stå tomt.

6. Åpne **192.168.0.1** i en nettleser. Velg det WiFiet du vil at IoT enheten skal kobles til, skriv inn passordet og legg inn connection stringen.

7. Du skal nå få opp at WiFi informasjonen og device connection stringen er lagret i MXChipen. MXChipen rebooter seg selv.

OBS! Det kan hende at det nå kjører et program på MXCHipen, hvor den leser Temperatur og Fuktighet (Humidity), men dette vil bli overskrevet så det er bare å overse.

## Sett opp prosjektet

### Opprett Azure Cognitive Service

- Gå inn på [Azure Portalen](https://portal.azure.com/), og klikk på **Create a Resource**, og søk på **Speech**.

- Opprett en Speech resource ved fylle ut de nødvendige feltene. Pass på å velge den ressursgruppen som du allerede har opprettet.

- Gå inn på Speech ressursen du nettopp opprettet. Klikk på **Keys** i margen, og kopier **Key1**.

NB! Anbefaler å bruke en region i nærheten av Norge, men kanskje ikke East/West Norway. Europa regionene er fine å bruke, eller f.eks. eastus.

### Benytt Speech Service i Azure Cognitive Service med Azure Functions

- Gå nå tilbake til Visual Studio Code og åpne filen som heter _`Functions\DevKitTranslatorFunction`_ og oppdater følgende linjer:

```
// Subscription Key of Speech Service
const string speechSubscriptionKey = "";

// Region of the speech service, see https://docs.microsoft.com/azure/cognitive-services/speech-service/regions for more details.
const string speechServiceRegion = "";

// Device ID
const string deviceName = "";
```

- Klikk `F1`, søk og velg **Azure IoT Device Workbench: Deploy to Azure...**. Hvis VSCode spør om bekrefelse, klikk **Yes**.

- Pass på at det kommer opp en liten meldingsboks i høyre hjørne om at deploymenten var suksessfull.

- Gå tilbake til Azure Portalen, og finn _Function Appen_ du nettopp har laget. Enten ved å gå inn i ressursgruppen, eller ved å klikke på _Function Apps_ i burgermenyen oppe i venstre hjørne.

- Klikk på **Functions** og deretter **devkit_translator**. Klikk deretter på **Get Function URL** for å kopiere URLen.

- Kopier inn URLen i `azure_config.h` filen i VSCode.

### Bygg og last opp device koden

Vi skal nå bygge og laste opp device koden på MXChipen.

- Start med å sette MXChipen i _configuration mode_ ved å holde inn knapp **A** og klikke på **Reset** knappen.

På skjermen skal du nå kunne se en _id_ og _Configuration_.

- Klikk `F1` i VSCode, søk og velg **Azure IoT Device Workbench: Configure Device Settings... > Config Device Connection String**. Velg **Select IoT Hub Device Connection String** for å konfigurere denne til MXChipen. Dersom du gjorde dette da du koblet chipen opp til WiFi så skal ikke dette steget være nødvendig.

- Klikk `F1` igjen, søk og velg **Azure IoT Device Workbench: Upload Device Code**. Du kan nå se i Output vinduet at koden kompileres og lastes opp til MXChipen.

### Test sample prosjektet

Hvis alt har gått knirkefritt skal du nå ha en applikasjon på chipen som lar deg snakke inn et språk du velger, oversetter det og skriver dette ut på engelsk.

NB! Azure Cognitive Services benytter seg av _Bing_, så oversettelsene må tas med en klype salt.

Denne applikasjonen kan brukes slik:

- Klikk A for å gå inn i setup mode.

- Klikk B for å scrolle over alle støttede språk.

- Klikk A for å velge språk.

- Trykk og hold B mens du snakker, slipp B for å initiere oversettelse.

- Den oversettede teksten dukker opp på skjermen.

# Oppgave: Gjør om til å oversette fra engelsk

De færreste av oss snakker et annet språk og vil ha det oversatt til engelsk. Vi vil heller snakke på engelsk å få det oversatt til et annet språk! Oppgaven deres blir derfor å gå inn i koden å gjøre endringer, og deploye på nytt, slik at man kan snakke på engelsk og få det oversatt til valgt språk.

Hint:

- Se på de tre hovedfilene: **DevKitTranslatorFunction.cs**, **SpeechTranslation.cs**, og **DevKitTranslator.cs**.
- En måte å gjøre det på er ved å se nærmere på funksjonen **TranslationWithAudioStreamAsync()**, og se på argumentene til funksjonen.
- NB! Skjermen på kortet er ikke så flink til å vise tegn som ikke er i det latinske alfabetet. Så det kan være en fordel å fjerne språkene som ikke bruker latinske bokstaver.
- Husk å laste opp kode til både Azure og til device.

# Oppgave: Gjør om koden til å bli en universal-oversetter

Gikk det altfor fort å endre på de få kodelinjene? Frykt ikke. Vi har oppgaven til deg. Vi vil nå at du går inn i koden igjen og gjør den om slik at man skal kunne velge hvilket språk man vil konvertere fra, og hvilket språk man vil konvertere til, før man oversetter.

Hint:

- Se
- Endre

Når du er ferdig med oppgaven er du ferdig med denne workshopen! Hurra! Håper du føler at du har lært noe og at dette har trigget nysgjerrigheten for IoT-løsninger. Håper å se deg igjen på neste IoT Workshop! Vi tar veldig gjerne tilbakemeldinger og ideer til andre ting vi kan gjøre.
