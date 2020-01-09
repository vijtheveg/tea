# Translation Editor for Android (tea)
A full-lifecycle translations editor for localizing Android applications.

Localizing an appliation is not an easy process:
1. The app developer creates the app in the default language, say English.
2. The English string files need to be sent to a translator.
    - _**Problem**: The translator may not be comfortable enlisting in your source code repository or with editing XML files. If your strings are in multiple XML files, sharing these files is particularly messy._
3. The translator needs to see the English strings side-by-side with the translated strings.
   - _**Problem**: Translators don't have Android Studio Translations Editor or similar tools._
4. The translator sends back the localized strings.
    - _**Problem**: How to create XML files from these localized strings?_
5. After a while the developer modifies the app and adds / deletes / changes a few strings.
    - _**Problem**: How to remove the deleted strings from all the translations?_
    - _**Problem**: How send the added / modified strings for re-translation?_
    - _**Problem**: How merge the new translations back into existing XML files?_

The Translation Editor for Android console application (`teac`) solves these problems.

## Step 0
Go to [Releases](https://github.com/vijtheveg/tea/releases) and download `teac.zip` for the latest available version. Extract `teac.exe` from this zip file into any directory that is in your %PATH% environment variable.

**NOTE**: Currently, `teac.exe` only works on Windows.

## Step 1
Generate an Excel spreadsheet that displays the original language strings and the translation side-by-side.

To do this, run `teac` with the `excel-export` command. **Important**: Make sure the current directory is the `res` directory of your android project!
```
C:\repo\my-android-app\app\src\main\res>teac.exe excel-export en kn en-to-kn.xlsx
```
In the example above, `en` is the source language (the language to translate from) and `kn` is the target language (the language to translate into).

The generated Excel file (`en-to-kn.xlsx`) looks like this:
<img align="left" style="margin:0px 15px 0px 0px" src="https://raw.githubusercontent.com/vijtheveg/tea/master/TranslationEditorForAndroid/Images/excel_file.jpg"/>

&nbsp;

## Step 2
Send the Excel file generated above to your translator.

The translator can only edit white cells (cream colored cells are read-only). The translator fills in translations. When a particular string has been translated to her satisfaction, have her mark the translation as final by entering a 'Y' in the last column (see cell `E4` in the image above).

Have the translator send the Excel file, with the translations filled, in back to you.

## Step 3
Import the translations back into XML files.

To do this, run `teac` with the `excel-import` command. **Important**: Make sure the current directory is the `res` directory of your android project!
```
C:\repo\my-android-app\app\src\main\res>teac.exe excel-import en kn en-to-kn.xlsx
```
**WARNING**:  This command will delete and recreate all XML files in the target directory (`values-kn` in the example above)!!

The generated XML will look like this:
<img align="left" style="margin:0px 15px 0px 0px" src="https://raw.githubusercontent.com/vijtheveg/tea/master/TranslationEditorForAndroid/Images/xml_file.jpg"/>

&nbsp;

**NOTE**: The comment above a string resource in the target language XML file that starts with `**DO NOT EDIT**` indicates that the translated string below it is final, and does not need to be looked at again (so, final strings will not be in Excel files generated in [Step 1](#step-1) above. Delete this comment line if you want to re-translate a string.

## Later
If you add / delete / modify any strings in the source language at the later date, just do Steps 1-3 above again. This time `teac` will generate an Excel file that only contains new / modified strings and strings that were not marked as final.

## Coming Soon
The ability to generate automatic translations. This can serve as a starting point for manual translations.
