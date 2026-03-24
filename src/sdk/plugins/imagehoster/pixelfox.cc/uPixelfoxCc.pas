unit uPixelfoxCc;

interface

uses
  // Delphi
  Windows, SysUtils, Classes, Variants, ActiveX,
  // HTTPManager
  uHTTPInterface, uHTTPClasses,
  // Plugin system
  uPlugInImageHosterClass, uPlugInHTTPClasses,
  // LkJSON
  uLkJSON;

type
  TPixelfoxCc = class(TImageHosterPlugIn)
  protected
  const
    WEBSITE = 'https://pixelfox.cc/';
    API_BASE = WEBSITE + 'api/v1/';
    STATUS_POLL_COUNT = 60;
    STATUS_POLL_DELAY_MS = 500;
  private
    function HasAPIKey: Boolean;
    function CreateAPIRequest(const APath: string): IHTTPRequest;
    function ExecuteRequest(const AHTTPRequest: IHTTPRequest; const AHTTPParams: IHTTPParams; out AHTTPProcess: IHTTPProcess): Boolean;
    function ExecuteJSONRequest(const AHTTPRequest: IHTTPRequest; const AHTTPParams: IHTTPParams; out AJSON: TlkJSONobject): Boolean;
    function GetAPIErrorMessage(const ASourceCode, AFallback: string): string;
    function TryGetObjectField(const AParent: TlkJSONbase; const AName: WideString; out AValue: TlkJSONobject): Boolean;
    function TryGetArrayField(const AParent: TlkJSONbase; const AName: WideString; out AValue: TlkJSONlist): Boolean;
    function TryGetStringField(const AParent: TlkJSONbase; const AName: WideString; out AValue: string): Boolean;
    function TryGetBooleanField(const AParent: TlkJSONbase; const AName: WideString; out AValue: Boolean): Boolean;
    function IsAllowedFormat(const AAllowedFormats: TlkJSONlist; const AFormat: string): Boolean;
    procedure AddDerivative(const ADerivatives: TlkJSONlist; const AFamily, ASize: string);
    function GetLocalFileSize(const ALocalPath: WideString): Int64;
    function LoadCapabilities(out AAllowOriginal, AAllowWebp, AAllowAvif: Boolean): Boolean;
    function CreateUploadSession(const ALocalPath: WideString; AAllowOriginal, AAllowWebp, AAllowAvif: Boolean; out AUploadURL, AUploadToken: string): Boolean;
    function UploadFile(const AUploadURL, AUploadToken: string; const ALocalPath: WideString; out AImageUUID: string; out AUploadResponse: TlkJSONobject): Boolean;
    function WaitForProcessing(const AImageUUID: string; out AComplete: Boolean): Boolean;
    function LoadImageResource(const AImageUUID: string; out AImageJSON: TlkJSONobject): Boolean;
    function TryGetVariantURL(const AJSON: TlkJSONobject; const ARootName, AFamily, ASize: string; ARequireReady: Boolean; out AURL: string): Boolean;
    function SelectPreferredURL(const AJSON: TlkJSONobject): string;
    function ExtractURLExtension(const AURL: WideString): string;
    function DownloadRemoteFile(const ARemoteURL: WideString; out ATempFile: string): Boolean;
  public
    function GetName: WideString; override;
    function LocalUpload(const ALocalPath: WideString; out AUrl: WideString): WordBool; override;
    function RemoteUpload(const ARemoteUrl: WideString; out AUrl: WideString): WordBool; override;
  end;

implementation

function TPixelfoxCc.HasAPIKey: Boolean;
begin
  Result := Trim(AccountName) <> '';
  if not Result then
    ErrorMsg := 'Pixelfox API key missing. Store the API key in the account name field.';
end;

function TPixelfoxCc.CreateAPIRequest(const APath: string): IHTTPRequest;
begin
  Result := THTTPRequest.Create(API_BASE + APath);
  with Result do
  begin
    Accept := 'application/json';
    Referer := WEBSITE;
    CustomHeaders.Add('X-API-Key: ' + AccountName);
  end;
end;

function TPixelfoxCc.ExecuteRequest(const AHTTPRequest: IHTTPRequest; const AHTTPParams: IHTTPParams; out AHTTPProcess: IHTTPProcess): Boolean;
var
  LRequestID: Double;
begin
  AHTTPProcess := nil;

  if Assigned(AHTTPParams) then
    LRequestID := HTTPManager.Post(AHTTPRequest, AHTTPParams, TPlugInHTTPOptions.Create(Self))
  else
    LRequestID := HTTPManager.Get(AHTTPRequest, TPlugInHTTPOptions.Create(Self));

  repeat
    Sleep(50);
  until HTTPManager.HasResult(LRequestID);

  AHTTPProcess := HTTPManager.GetResult(LRequestID);
  Result := Assigned(AHTTPProcess);
  if not Result then
    ErrorMsg := 'Pixelfox request returned no HTTP process result.';
end;

function TPixelfoxCc.ExecuteJSONRequest(const AHTTPRequest: IHTTPRequest; const AHTTPParams: IHTTPParams; out AJSON: TlkJSONobject): Boolean;
var
  LHTTPProcess: IHTTPProcess;
  LSourceCode: string;
begin
  Result := False;
  AJSON := nil;

  if not ExecuteRequest(AHTTPRequest, AHTTPParams, LHTTPProcess) then
    Exit;

  LSourceCode := string(LHTTPProcess.HTTPResult.SourceCode);

  if LHTTPProcess.HTTPResult.HasError or (LHTTPProcess.HTTPResult.HTTPResponse.Code >= 400) then
  begin
    ErrorMsg := GetAPIErrorMessage(LSourceCode, LHTTPProcess.HTTPResult.HTTPResponseInfo.ErrorMessage);
    Exit;
  end;

  try
    AJSON := TlkJSON.ParseText(LSourceCode) as TlkJSONobject;
    Result := Assigned(AJSON);
    if not Result then
      ErrorMsg := 'Pixelfox returned a non-object JSON response.';
  except
    on E: Exception do
    begin
      ErrorMsg := 'Pixelfox returned invalid JSON: ' + E.Message;
    end;
  end;
end;

function TPixelfoxCc.GetAPIErrorMessage(const ASourceCode, AFallback: string): string;
var
  LJSON: TlkJSONobject;
  LMessage: string;
  LDetails: string;
  LError: string;
begin
  Result := AFallback;

  try
    LJSON := TlkJSON.ParseText(ASourceCode) as TlkJSONobject;
    try
      if TryGetStringField(LJSON, 'message', LMessage) then
        Result := LMessage;

      if TryGetStringField(LJSON, 'details', LDetails) then
      begin
        if Result = '' then
          Result := LDetails
        else if Pos(LDetails, Result) = 0 then
          Result := Result + ': ' + LDetails;
      end;

      if (Result = '') and TryGetStringField(LJSON, 'error', LError) then
        Result := LError;
    finally
      LJSON.Free;
    end;
  except
  end;

  if Result = '' then
    Result := 'Pixelfox request failed.';
end;

function TPixelfoxCc.TryGetObjectField(const AParent: TlkJSONbase; const AName: WideString; out AValue: TlkJSONobject): Boolean;
var
  LField: TlkJSONbase;
begin
  AValue := nil;
  Result := Assigned(AParent);
  if not Result then
    Exit;

  LField := AParent.Field[AName];
  Result := Assigned(LField) and (LField is TlkJSONobject);
  if Result then
    AValue := LField as TlkJSONobject;
end;

function TPixelfoxCc.TryGetArrayField(const AParent: TlkJSONbase; const AName: WideString; out AValue: TlkJSONlist): Boolean;
var
  LField: TlkJSONbase;
begin
  AValue := nil;
  Result := Assigned(AParent);
  if not Result then
    Exit;

  LField := AParent.Field[AName];
  Result := Assigned(LField) and (LField is TlkJSONlist);
  if Result then
    AValue := LField as TlkJSONlist;
end;

function TPixelfoxCc.TryGetStringField(const AParent: TlkJSONbase; const AName: WideString; out AValue: string): Boolean;
var
  LField: TlkJSONbase;
begin
  AValue := '';
  Result := Assigned(AParent);
  if not Result then
    Exit;

  LField := AParent.Field[AName];
  Result := Assigned(LField);
  if Result then
    AValue := Trim(VarToStr(LField.Value));
end;

function TPixelfoxCc.TryGetBooleanField(const AParent: TlkJSONbase; const AName: WideString; out AValue: Boolean): Boolean;
var
  LField: TlkJSONbase;
  LValue: string;
begin
  AValue := False;
  Result := Assigned(AParent);
  if not Result then
    Exit;

  LField := AParent.Field[AName];
  Result := Assigned(LField);
  if Result then
  begin
    LValue := LowerCase(Trim(VarToStr(LField.Value)));
    AValue := (LValue = 'true') or (LValue = '1');
  end;
end;

function TPixelfoxCc.IsAllowedFormat(const AAllowedFormats: TlkJSONlist; const AFormat: string): Boolean;
var
  I: Integer;
begin
  Result := False;

  if not Assigned(AAllowedFormats) then
    Exit;

  for I := 0 to AAllowedFormats.Count - 1 do
    if SameText(VarToStr(AAllowedFormats.Child[I].Value), AFormat) then
    begin
      Result := True;
      Exit;
    end;
end;

procedure TPixelfoxCc.AddDerivative(const ADerivatives: TlkJSONlist; const AFamily, ASize: string);
var
  LDerivative: TlkJSONobject;
begin
  LDerivative := TlkJSONobject.Create;
  LDerivative.Add('family', AFamily);
  LDerivative.Add('size', ASize);
  ADerivatives.Add(LDerivative);
end;

function TPixelfoxCc.GetLocalFileSize(const ALocalPath: WideString): Int64;
var
  LFileStream: TFileStream;
begin
  LFileStream := TFileStream.Create(ALocalPath, fmOpenRead or fmShareDenyNone);
  try
    Result := LFileStream.Size;
  finally
    LFileStream.Free;
  end;
end;

function TPixelfoxCc.LoadCapabilities(out AAllowOriginal, AAllowWebp, AAllowAvif: Boolean): Boolean;
var
  LProfileJSON: TlkJSONobject;
  LLimitsJSON: TlkJSONobject;
  LAllowedFormats: TlkJSONlist;
  LImageUploadEnabled: Boolean;
  LDirectUploadEnabled: Boolean;
begin
  Result := False;
  AAllowOriginal := False;
  AAllowWebp := False;
  AAllowAvif := False;

  if not ExecuteJSONRequest(CreateAPIRequest('user/profile'), nil, LProfileJSON) then
    Exit;

  try
    if not TryGetObjectField(LProfileJSON, 'limits', LLimitsJSON) then
    begin
      ErrorMsg := 'Pixelfox profile response is missing limits.';
      Exit;
    end;

    if TryGetBooleanField(LLimitsJSON, 'image_upload_enabled', LImageUploadEnabled) and not LImageUploadEnabled then
    begin
      ErrorMsg := 'Pixelfox image uploads are disabled for this account.';
      Exit;
    end;

    if TryGetBooleanField(LLimitsJSON, 'direct_upload_enabled', LDirectUploadEnabled) and not LDirectUploadEnabled then
    begin
      ErrorMsg := 'Pixelfox direct uploads are disabled for this account.';
      Exit;
    end;

    if TryGetArrayField(LLimitsJSON, 'allowed_thumbnail_formats', LAllowedFormats) then
    begin
      AAllowOriginal := IsAllowedFormat(LAllowedFormats, 'original');
      AAllowWebp := IsAllowedFormat(LAllowedFormats, 'webp');
      AAllowAvif := IsAllowedFormat(LAllowedFormats, 'avif');
    end;

    Result := True;
  finally
    LProfileJSON.Free;
  end;
end;

function TPixelfoxCc.CreateUploadSession(const ALocalPath: WideString; AAllowOriginal, AAllowWebp, AAllowAvif: Boolean; out AUploadURL, AUploadToken: string): Boolean;
var
  LRequestJSON: TlkJSONobject;
  LProcessingJSON: TlkJSONobject;
  LDerivatives: TlkJSONlist;
  LRequestBody: string;
  LRequest: IHTTPRequest;
  LHTTPParams: IHTTPParams;
  LResponseJSON: TlkJSONobject;
begin
  Result := False;
  AUploadURL := '';
  AUploadToken := '';

  LRequestJSON := TlkJSONobject.Create;
  try
    LRequestJSON.Add('file_size', Integer(GetLocalFileSize(ALocalPath)));

    LProcessingJSON := TlkJSONobject.Create;
    if AAllowOriginal or AAllowWebp or AAllowAvif then
    begin
      LProcessingJSON.Add('profile', 'custom');
      LDerivatives := TlkJSONlist.Create;

      if AAllowAvif then
        AddDerivative(LDerivatives, 'avif', 'medium');
      if AAllowWebp then
        AddDerivative(LDerivatives, 'webp', 'medium');
      if AAllowOriginal then
        AddDerivative(LDerivatives, 'original', 'medium');

      LProcessingJSON.Add('derivatives', LDerivatives);
    end
    else
    begin
      LProcessingJSON.Add('profile', 'original_only');
    end;

    LRequestJSON.Add('processing', LProcessingJSON);
    LRequestBody := TlkJSON.GenerateText(LRequestJSON);
  finally
    LRequestJSON.Free;
  end;

  LRequest := CreateAPIRequest('upload/sessions');
  LRequest.ContentType := 'application/json';
  LHTTPParams := THTTPParams.Create(LRequestBody);

  if not ExecuteJSONRequest(LRequest, LHTTPParams, LResponseJSON) then
    Exit;

  try
    Result := TryGetStringField(LResponseJSON, 'upload_url', AUploadURL)
      and TryGetStringField(LResponseJSON, 'token', AUploadToken);

    if not Result then
      ErrorMsg := 'Pixelfox upload session response is missing upload_url or token.';
  finally
    LResponseJSON.Free;
  end;
end;

function TPixelfoxCc.UploadFile(const AUploadURL, AUploadToken: string; const ALocalPath: WideString; out AImageUUID: string; out AUploadResponse: TlkJSONobject): Boolean;
var
  LRequest: IHTTPRequest;
  LHTTPParams: IHTTPParams;
begin
  Result := False;
  AImageUUID := '';
  AUploadResponse := nil;

  LRequest := THTTPRequest.Create(AUploadURL);
  with LRequest do
  begin
    Accept := 'application/json';
    Referer := WEBSITE;
    CustomHeaders.Add('Authorization: Bearer ' + AUploadToken);
  end;

  LHTTPParams := THTTPParams.Create;
  LHTTPParams.AddFile('file', ALocalPath);

  if not ExecuteJSONRequest(LRequest, LHTTPParams, AUploadResponse) then
    Exit;

  Result := TryGetStringField(AUploadResponse, 'image_uuid', AImageUUID);
  if not Result then
  begin
    ErrorMsg := 'Pixelfox upload response is missing image_uuid.';
    FreeAndNil(AUploadResponse);
  end;
end;

function TPixelfoxCc.WaitForProcessing(const AImageUUID: string; out AComplete: Boolean): Boolean;
var
  I: Integer;
  LStatusJSON: TlkJSONobject;
  LComplete: Boolean;
  LFailed: Boolean;
begin
  Result := True;
  AComplete := False;

  for I := 0 to STATUS_POLL_COUNT - 1 do
  begin
    if not ExecuteJSONRequest(CreateAPIRequest('images/' + AImageUUID + '/status'), nil, LStatusJSON) then
    begin
      Result := False;
      Exit;
    end;

    try
      LComplete := False;
      LFailed := False;

      TryGetBooleanField(LStatusJSON, 'complete', LComplete);
      TryGetBooleanField(LStatusJSON, 'failed', LFailed);

      if LFailed then
      begin
        ErrorMsg := 'Pixelfox image processing failed.';
        Result := False;
        Exit;
      end;

      if LComplete then
      begin
        AComplete := True;
        Result := True;
        Exit;
      end;
    finally
      LStatusJSON.Free;
    end;

    Sleep(STATUS_POLL_DELAY_MS);
  end;
end;

function TPixelfoxCc.LoadImageResource(const AImageUUID: string; out AImageJSON: TlkJSONobject): Boolean;
begin
  Result := ExecuteJSONRequest(CreateAPIRequest('images/' + AImageUUID), nil, AImageJSON);
end;

function TPixelfoxCc.TryGetVariantURL(const AJSON: TlkJSONobject; const ARootName, AFamily, ASize: string; ARequireReady: Boolean; out AURL: string): Boolean;
var
  LRootJSON: TlkJSONobject;
  LFamilyJSON: TlkJSONobject;
  LSizeJSON: TlkJSONobject;
  LReady: Boolean;
begin
  AURL := '';

  Result := TryGetObjectField(AJSON, ARootName, LRootJSON)
    and TryGetObjectField(LRootJSON, AFamily, LFamilyJSON)
    and TryGetObjectField(LFamilyJSON, ASize, LSizeJSON)
    and TryGetStringField(LSizeJSON, 'url', AURL);

  if Result and ARequireReady then
  begin
    if TryGetBooleanField(LSizeJSON, 'ready', LReady) then
      Result := LReady;
  end;
end;

function TPixelfoxCc.SelectPreferredURL(const AJSON: TlkJSONobject): string;
begin
  Result := '';

  if TryGetVariantURL(AJSON, 'stable_variants', 'avif', 'medium', True, Result) then
    Exit;
  if TryGetVariantURL(AJSON, 'variants', 'avif', 'medium', False, Result) then
    Exit;

  if TryGetVariantURL(AJSON, 'stable_variants', 'webp', 'medium', True, Result) then
    Exit;
  if TryGetVariantURL(AJSON, 'variants', 'webp', 'medium', False, Result) then
    Exit;

  if TryGetVariantURL(AJSON, 'stable_variants', 'original', 'medium', True, Result) then
    Exit;
  if TryGetVariantURL(AJSON, 'variants', 'original', 'medium', False, Result) then
    Exit;

  if TryGetStringField(AJSON, 'stable_url', Result) and (Result <> '') then
    Exit;
  if TryGetStringField(AJSON, 'url', Result) and (Result <> '') then
    Exit;

  if TryGetVariantURL(AJSON, 'stable_variants', 'original', 'original', True, Result) then
    Exit;
  if TryGetVariantURL(AJSON, 'variants', 'original', 'original', False, Result) then
    Exit;

  Result := '';
end;

function TPixelfoxCc.ExtractURLExtension(const AURL: WideString): string;
var
  LValue: string;
  LQueryPos: Integer;
  LHashPos: Integer;
begin
  LValue := string(AURL);

  LQueryPos := Pos('?', LValue);
  if LQueryPos > 0 then
    LValue := Copy(LValue, 1, LQueryPos - 1);

  LHashPos := Pos('#', LValue);
  if LHashPos > 0 then
    LValue := Copy(LValue, 1, LHashPos - 1);

  Result := ExtractFileExt(LValue);
  if (Result = '') or (Length(Result) > 8) then
    Result := '.jpg';
end;

function TPixelfoxCc.DownloadRemoteFile(const ARemoteURL: WideString; out ATempFile: string): Boolean;
var
  LTempPath: array[0..MAX_PATH] of Char;
  LTempFileName: array[0..MAX_PATH] of Char;
  LDownloadedFile: string;
  LRequestID: Double;
  LHTTPProcess: IHTTPProcess;
  LOleStream: TOleStream;
  LDummy: Int64;
  LFileStream: TFileStream;
begin
  Result := False;
  ATempFile := '';

  if Windows.GetTempPath(MAX_PATH, LTempPath) = 0 then
  begin
    ErrorMsg := 'Could not resolve a temporary directory for Pixelfox remote upload.';
    Exit;
  end;

  if Windows.GetTempFileName(LTempPath, 'pfx', 0, LTempFileName) = 0 then
  begin
    ErrorMsg := 'Could not create a temporary file for Pixelfox remote upload.';
    Exit;
  end;

  LDownloadedFile := ChangeFileExt(string(LTempFileName), ExtractURLExtension(ARemoteURL));
  SysUtils.DeleteFile(string(LTempFileName));

  LRequestID := HTTPManager.Get(THTTPRequest.Create(ARemoteURL), TPlugInHTTPOptions.Create(Self));

  repeat
    Sleep(50);
  until HTTPManager.HasResult(LRequestID);

  LHTTPProcess := HTTPManager.GetResult(LRequestID);
  if not Assigned(LHTTPProcess) or LHTTPProcess.HTTPResult.HasError or (LHTTPProcess.HTTPResult.HTTPResponse.Code >= 400) then
  begin
    ErrorMsg := 'Pixelfox remote upload could not download the source URL first.';
    if FileExists(LDownloadedFile) then
      SysUtils.DeleteFile(LDownloadedFile);
    Exit;
  end;

  LOleStream := TOleStream.Create(LHTTPProcess.HTTPResult.HTTPResponse.ContentStream);
  try
    LHTTPProcess.HTTPResult.HTTPResponse.ContentStream.Seek(0, STREAM_SEEK_SET, LDummy);
    LOleStream.Seek(0, STREAM_SEEK_SET);

    LFileStream := TFileStream.Create(LDownloadedFile, fmCreate);
    try
      LFileStream.CopyFrom(LOleStream, LOleStream.Size);
    finally
      LFileStream.Free;
    end;
  finally
    LOleStream.Free;
  end;

  ATempFile := LDownloadedFile;
  Result := True;
end;

function TPixelfoxCc.GetName: WideString;
begin
  Result := 'Pixelfox.cc';
end;

function TPixelfoxCc.LocalUpload(const ALocalPath: WideString; out AUrl: WideString): WordBool;
var
  LAllowOriginal: Boolean;
  LAllowWebp: Boolean;
  LAllowAvif: Boolean;
  LUploadURL: string;
  LUploadToken: string;
  LImageUUID: string;
  LUploadResponse: TlkJSONobject;
  LImageJSON: TlkJSONobject;
  LComplete: Boolean;
begin
  Result := False;
  AUrl := '';

  if not HasAPIKey then
    Exit;

  if not FileExists(ALocalPath) then
  begin
    ErrorMsg := 'Local image file not found: ' + ALocalPath;
    Exit;
  end;

  if not LoadCapabilities(LAllowOriginal, LAllowWebp, LAllowAvif) then
    Exit;

  if not CreateUploadSession(ALocalPath, LAllowOriginal, LAllowWebp, LAllowAvif, LUploadURL, LUploadToken) then
    Exit;

  if not UploadFile(LUploadURL, LUploadToken, ALocalPath, LImageUUID, LUploadResponse) then
    Exit;

  try
    AUrl := SelectPreferredURL(LUploadResponse);

    if not WaitForProcessing(LImageUUID, LComplete) then
      Exit;

    if LComplete and LoadImageResource(LImageUUID, LImageJSON) then
      try
        AUrl := SelectPreferredURL(LImageJSON);
      finally
        LImageJSON.Free;
      end;

    if AUrl = '' then
      AUrl := SelectPreferredURL(LUploadResponse);

    Result := AUrl <> '';
    if not Result then
      ErrorMsg := 'Pixelfox did not return a usable image URL.';
  finally
    LUploadResponse.Free;
  end;
end;

function TPixelfoxCc.RemoteUpload(const ARemoteUrl: WideString; out AUrl: WideString): WordBool;
var
  LTempFile: string;
begin
  Result := False;
  AUrl := '';
  LTempFile := '';

  if not DownloadRemoteFile(ARemoteUrl, LTempFile) then
    Exit;

  try
    Result := LocalUpload(LTempFile, AUrl);
  finally
    if (LTempFile <> '') and FileExists(LTempFile) then
      SysUtils.DeleteFile(LTempFile);
  end;
end;

end.
