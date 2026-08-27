/* =====================================================================
   Alianzagrafica — Sistema de Evaluación de Desempeño 180°
   Script de base de datos (Microsoft SQL Server / T-SQL)

   CONTENIDO:
     0. Creación de base de datos
     1. Esquema (DDL) — corresponde al diccionario de tablas de la
        sección 9.2 del Documento de Requerimientos y Diseño v1.0
     2. Catálogos base (TipoPersonal, Rol)
     3. Tabla de EMPLEADOS FICTICIA (dbo.Empleado) — ver nota abajo
     4. Usuarios y roles
     5. Competencias (propuesta inicial por tipo de personal)
     6. Periodo de evaluación 2026 + formularios + ponderación
     7. Generación automática de asignaciones evaluador-evaluado
        (implementa la lógica de RF-09 a partir de la jerarquía)
     8. Caso de ejemplo resuelto (una evaluación completa y consolidada)
     9. Consultas de verificación / ejemplos de reporte

   NOTA IMPORTANTE SOBRE dbo.Empleado:
     Esta tabla es un SUSTITUTO FICTICIO de la información real de
     empleados, que en producción proviene de Novasoft (ver sección 8.4
     del documento de diseño: Estrategia A — vista de solo lectura, o
     Estrategia B — sincronización periódica). Se crea aquí como tabla
     física únicamente para poder avanzar con el modelo de datos y
     probar la lógica de negocio mientras se confirma el esquema real
     de Novasoft con el equipo de TI de Alianzagrafica. Cuando esa
     información esté disponible, esta tabla se reemplaza por la vista
     o la tabla espejo correspondiente, manteniendo el mismo contrato
     de columnas (o un adaptador que lo respete) para no impactar el
     resto del modelo.
   ===================================================================== */

-- =========================================================
-- 0. BASE DE DATOS
-- =========================================================
IF DB_ID(N'EvaluacionDesempeno180') IS NULL
BEGIN
    CREATE DATABASE EvaluacionDesempeno180;
END
GO

USE EvaluacionDesempeno180;
GO

-- =========================================================
-- 1. ESQUEMA (DDL)
-- =========================================================

-- ---- Catálogo de tipos de personal ----
CREATE TABLE dbo.TipoPersonal (
    IdTipoPersonal              INT IDENTITY(1,1)   NOT NULL,
    Nombre                      NVARCHAR(50)        NOT NULL,
    PermiteEvaluacionAscendente BIT                 NOT NULL CONSTRAINT DF_TipoPersonal_Ascendente DEFAULT (0),
    CONSTRAINT PK_TipoPersonal PRIMARY KEY (IdTipoPersonal),
    CONSTRAINT UQ_TipoPersonal_Nombre UNIQUE (Nombre)
);
GO

-- ---- Empleado (FICTICIO — ver nota al inicio del script) ----
CREATE TABLE dbo.Empleado (
    CodigoEmpleado       INT            NOT NULL,   -- equivalente al ID del empleado en Novasoft
    NumeroIdentificacion VARCHAR(20)    NOT NULL,
    Nombre               NVARCHAR(150)  NOT NULL,
    Cargo                NVARCHAR(100)  NOT NULL,
    Area                 NVARCHAR(100)  NOT NULL,
    IdTipoPersonal       INT            NOT NULL,
    CodigoJefeDirecto    INT            NULL,
    CorreoElectronico    NVARCHAR(150)  NULL,
    Estado               VARCHAR(10)    NOT NULL CONSTRAINT DF_Empleado_Estado DEFAULT ('Activo'),
    FechaIngreso         DATE           NOT NULL,
    FechaSincronizacion  DATETIME2(0)   NOT NULL CONSTRAINT DF_Empleado_FechaSync DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Empleado PRIMARY KEY (CodigoEmpleado),
    CONSTRAINT UQ_Empleado_Identificacion UNIQUE (NumeroIdentificacion),
    CONSTRAINT FK_Empleado_TipoPersonal FOREIGN KEY (IdTipoPersonal) REFERENCES dbo.TipoPersonal (IdTipoPersonal),
    CONSTRAINT FK_Empleado_Jefe FOREIGN KEY (CodigoJefeDirecto) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT CK_Empleado_Estado CHECK (Estado IN ('Activo', 'Inactivo'))
);
GO

-- ---- Roles funcionales del sistema (sección 4.2) ----
CREATE TABLE dbo.Rol (
    IdRol       INT IDENTITY(1,1)  NOT NULL,
    NombreRol   NVARCHAR(50)       NOT NULL,
    Descripcion NVARCHAR(250)      NULL,
    CONSTRAINT PK_Rol PRIMARY KEY (IdRol),
    CONSTRAINT UQ_Rol_Nombre UNIQUE (NombreRol)
);
GO

-- ---- Usuarios del sistema (cuenta de acceso, vinculada a un empleado) ----
CREATE TABLE dbo.Usuario (
    IdUsuario         INT IDENTITY(1,1)  NOT NULL,
    CodigoEmpleado    INT                NOT NULL,
    NombreUsuario     NVARCHAR(100)      NOT NULL,
    TipoAutenticacion VARCHAR(20)        NOT NULL CONSTRAINT DF_Usuario_TipoAuth DEFAULT ('ActiveDirectory'),
    ClaveHash         VARBINARY(256)     NULL,       -- solo aplica si TipoAutenticacion = 'Local'
    Activo            BIT                NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT (1),
    FechaCreacion     DATETIME2(0)       NOT NULL CONSTRAINT DF_Usuario_FechaCreacion DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Usuario PRIMARY KEY (IdUsuario),
    CONSTRAINT UQ_Usuario_Empleado UNIQUE (CodigoEmpleado),
    CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT FK_Usuario_Empleado FOREIGN KEY (CodigoEmpleado) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT CK_Usuario_TipoAuth CHECK (TipoAutenticacion IN ('ActiveDirectory', 'Local'))
);
GO

CREATE TABLE dbo.UsuarioRol (
    IdUsuario INT NOT NULL,
    IdRol     INT NOT NULL,
    CONSTRAINT PK_UsuarioRol PRIMARY KEY (IdUsuario, IdRol),
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY (IdRol) REFERENCES dbo.Rol (IdRol)
);
GO

-- ---- Competencias evaluables ----
CREATE TABLE dbo.Competencia (
    IdCompetencia  INT IDENTITY(1,1)  NOT NULL,
    Nombre         NVARCHAR(150)      NOT NULL,
    Descripcion    NVARCHAR(400)      NULL,
    IdTipoPersonal INT                NULL,          -- NULL = competencia genérica (aplica a todos)
    Categoria      VARCHAR(20)        NULL,          -- 'Organizacional' | 'DeRol' (RF-07, macro-grupos de ponderación — ver GHU-FOR-007). NULL = sin macro-grupo (reparto parejo 100%/N, comportamiento histórico).
    Activa         BIT                NOT NULL CONSTRAINT DF_Competencia_Activa DEFAULT (1),
    CONSTRAINT PK_Competencia PRIMARY KEY (IdCompetencia),
    CONSTRAINT FK_Competencia_TipoPersonal FOREIGN KEY (IdTipoPersonal) REFERENCES dbo.TipoPersonal (IdTipoPersonal),
    CONSTRAINT CK_Competencia_Categoria CHECK (Categoria IN ('Organizacional', 'DeRol') OR Categoria IS NULL)
);
GO

-- ---- Comportamientos observables de cada competencia (Entregable 13 — columna
-- "COMPORTAMIENTOS" del formato real "EVALUACION DESEMPEÑO_Evaluaciones" de Alianzagrafica). El
-- evaluador califica cada comportamiento individualmente en % (0-100); dbo.RespuestaDetalle.
-- Calificacion (la "NOTA FINAL" de la competencia) es el promedio simple de los comportamientos
-- respondidos de esa competencia — ver dbo.RespuestaComportamientoDetalle más abajo. El promedio
-- se calcula en el servidor (EvaluacionesController.Guardar en la aplicación), nunca a partir de
-- un valor que venga directamente del formulario posteado.
CREATE TABLE dbo.Comportamiento (
    IdComportamiento INT IDENTITY(1,1)  NOT NULL,
    IdCompetencia    INT                NOT NULL,
    Descripcion      NVARCHAR(400)      NOT NULL,
    Orden            INT                NOT NULL CONSTRAINT DF_Comportamiento_Orden DEFAULT (0),
    Activo           BIT                NOT NULL CONSTRAINT DF_Comportamiento_Activo DEFAULT (1),
    CONSTRAINT PK_Comportamiento PRIMARY KEY (IdComportamiento),
    CONSTRAINT FK_Comportamiento_Competencia FOREIGN KEY (IdCompetencia) REFERENCES dbo.Competencia (IdCompetencia)
);
GO

-- ---- Indicadores de gestión (Entregable 11 — macro-grupo "Indicadores de Gestión", formato
-- real "EVALUACION DESEMPEÑO Indicadores" de Alianzagrafica). Un indicador se mide con una Meta
-- y un Resultado del mes, ambos en puntos porcentuales — ver dbo.RespuestaIndicadorDetalle más
-- abajo. Desde el Entregable 12 las competencias (dbo.Competencia) también se califican en % (ya
-- no de 1 a 5), así que ambos tipos de ítem comparten la misma escala nativa 0-100.
CREATE TABLE dbo.IndicadorGestion (
    IdIndicador    INT IDENTITY(1,1)  NOT NULL,
    Nombre         NVARCHAR(150)      NOT NULL,
    Formula        NVARCHAR(400)      NULL,           -- descripción de la fórmula/definición del indicador
    Ponderacion    DECIMAL(6,3)       NOT NULL,        -- peso DENTRO del grupo (no del total), en % (ej. 33.33)
    -- Meta fija del indicador, en puntos porcentuales (ej. 90 = 90%) (Entregable 12 — a pedido
    -- explícito del usuario, la Meta dejó de ser un valor que el evaluador escribe cada vez y
    -- pasó a ser un valor fijo del catálogo, igual para todas las evaluaciones mientras no se
    -- cambie aquí; ver Alianzagrafica.Evaluacion180.Web.Models.Entidades.IndicadorGestion.Meta).
    Meta           DECIMAL(5,2)       NOT NULL,
    IdTipoPersonal INT                NULL,            -- NULL = indicador genérico (aplica a todos)
    Activa         BIT                NOT NULL CONSTRAINT DF_IndicadorGestion_Activa DEFAULT (1),
    CONSTRAINT PK_IndicadorGestion PRIMARY KEY (IdIndicador),
    CONSTRAINT FK_IndicadorGestion_TipoPersonal FOREIGN KEY (IdTipoPersonal) REFERENCES dbo.TipoPersonal (IdTipoPersonal)
);
GO

-- ---- Periodos de evaluación ----
CREATE TABLE dbo.PeriodoEvaluacion (
    IdPeriodo     INT IDENTITY(1,1)  NOT NULL,
    Nombre        NVARCHAR(100)      NOT NULL,
    FechaApertura DATE               NOT NULL,
    FechaCierre   DATE               NOT NULL,
    Estado        VARCHAR(20)        NOT NULL CONSTRAINT DF_Periodo_Estado DEFAULT ('Programado'),
    CONSTRAINT PK_PeriodoEvaluacion PRIMARY KEY (IdPeriodo),
    CONSTRAINT UQ_Periodo_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Periodo_Estado CHECK (Estado IN ('Programado', 'Abierto', 'Cerrado')),
    CONSTRAINT CK_Periodo_Fechas CHECK (FechaCierre > FechaApertura)
);
GO

-- ---- Formularios de evaluación (por periodo, tipo de relación y tipo de personal) ----
CREATE TABLE dbo.FormularioEvaluacion (
    IdFormulario   INT IDENTITY(1,1)  NOT NULL,
    IdPeriodo      INT                NOT NULL,
    TipoRelacion   VARCHAR(20)        NOT NULL,   -- Autoevaluacion | Jefe | Ascendente
    IdTipoPersonal INT                NOT NULL,
    Nombre         NVARCHAR(150)      NOT NULL,
    CONSTRAINT PK_FormularioEvaluacion PRIMARY KEY (IdFormulario),
    CONSTRAINT UQ_Formulario UNIQUE (IdPeriodo, TipoRelacion, IdTipoPersonal),
    CONSTRAINT FK_Formulario_Periodo FOREIGN KEY (IdPeriodo) REFERENCES dbo.PeriodoEvaluacion (IdPeriodo),
    CONSTRAINT FK_Formulario_TipoPersonal FOREIGN KEY (IdTipoPersonal) REFERENCES dbo.TipoPersonal (IdTipoPersonal),
    CONSTRAINT CK_Formulario_TipoRelacion CHECK (TipoRelacion IN ('Autoevaluacion', 'Jefe', 'Ascendente'))
);
GO

CREATE TABLE dbo.FormularioCompetencia (
    IdFormulario  INT             NOT NULL,
    IdCompetencia INT             NOT NULL,
    Ponderacion   DECIMAL(5,2)    NOT NULL CONSTRAINT DF_FormCompetencia_Ponderacion DEFAULT (0),
    CONSTRAINT PK_FormularioCompetencia PRIMARY KEY (IdFormulario, IdCompetencia),
    CONSTRAINT FK_FormComp_Formulario FOREIGN KEY (IdFormulario) REFERENCES dbo.FormularioEvaluacion (IdFormulario),
    CONSTRAINT FK_FormComp_Competencia FOREIGN KEY (IdCompetencia) REFERENCES dbo.Competencia (IdCompetencia)
);
GO

-- Entregable 11: análoga a FormularioCompetencia, pero para indicadores de gestión.
CREATE TABLE dbo.FormularioIndicador (
    IdFormulario INT           NOT NULL,
    IdIndicador  INT           NOT NULL,
    Ponderacion  DECIMAL(5,2)  NOT NULL CONSTRAINT DF_FormIndicador_Ponderacion DEFAULT (0),
    CONSTRAINT PK_FormularioIndicador PRIMARY KEY (IdFormulario, IdIndicador),
    CONSTRAINT FK_FormInd_Formulario FOREIGN KEY (IdFormulario) REFERENCES dbo.FormularioEvaluacion (IdFormulario),
    CONSTRAINT FK_FormInd_Indicador FOREIGN KEY (IdIndicador) REFERENCES dbo.IndicadorGestion (IdIndicador)
);
GO

-- ---- Asignación evaluador -> evaluado ----
CREATE TABLE dbo.AsignacionEvaluacion (
    IdAsignacion     INT IDENTITY(1,1)  NOT NULL,
    IdPeriodo        INT                NOT NULL,
    CodigoEvaluador  INT                NOT NULL,
    CodigoEvaluado   INT                NOT NULL,
    TipoRelacion     VARCHAR(20)        NOT NULL,   -- Autoevaluacion | Jefe | Ascendente
    IdFormulario     INT                NULL,
    Estado           VARCHAR(20)        NOT NULL CONSTRAINT DF_Asignacion_Estado DEFAULT ('Programada'),
    CONSTRAINT PK_AsignacionEvaluacion PRIMARY KEY (IdAsignacion),
    CONSTRAINT UQ_Asignacion UNIQUE (IdPeriodo, CodigoEvaluador, CodigoEvaluado, TipoRelacion),
    CONSTRAINT FK_Asignacion_Periodo FOREIGN KEY (IdPeriodo) REFERENCES dbo.PeriodoEvaluacion (IdPeriodo),
    CONSTRAINT FK_Asignacion_Evaluador FOREIGN KEY (CodigoEvaluador) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT FK_Asignacion_Evaluado FOREIGN KEY (CodigoEvaluado) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT FK_Asignacion_Formulario FOREIGN KEY (IdFormulario) REFERENCES dbo.FormularioEvaluacion (IdFormulario),
    CONSTRAINT CK_Asignacion_TipoRelacion CHECK (TipoRelacion IN ('Autoevaluacion', 'Jefe', 'Ascendente')),
    CONSTRAINT CK_Asignacion_Estado CHECK (Estado IN ('Programada', 'Notificada', 'EnProceso', 'Completada'))
);
GO

-- ---- Respuestas de evaluación ----
CREATE TABLE dbo.RespuestaEvaluacion (
    IdRespuesta         INT IDENTITY(1,1)  NOT NULL,
    IdAsignacion        INT                NOT NULL,
    FechaEnvio          DATETIME2(0)       NULL,
    Estado              VARCHAR(10)        NOT NULL CONSTRAINT DF_Respuesta_Estado DEFAULT ('Borrador'),
    -- Sección "COMPROMISOS" (Entregable 11 — formato real "EVALUACION DESEMPEÑO Indicadores"):
    -- tres campos de texto libre diligenciados entre evaluador y evaluado.
    OportunidadesMejora NVARCHAR(2000)     NULL,
    Compromisos         NVARCHAR(2000)     NULL,
    RevisionCompromisos NVARCHAR(2000)     NULL,
    CONSTRAINT PK_RespuestaEvaluacion PRIMARY KEY (IdRespuesta),
    CONSTRAINT UQ_Respuesta_Asignacion UNIQUE (IdAsignacion),
    CONSTRAINT FK_Respuesta_Asignacion FOREIGN KEY (IdAsignacion) REFERENCES dbo.AsignacionEvaluacion (IdAsignacion),
    CONSTRAINT CK_Respuesta_Estado CHECK (Estado IN ('Borrador', 'Enviada'))
);
GO

-- Calificacion: % (0-100), NO 1-5 desde el Entregable 12 (a pedido explícito del usuario: "Todas
-- las calificaciones en todos los aspectos que hace el evaluador son en porcentajes, en donde la
-- calificacion minima es 0 y la maxima 100"). Antes era TINYINT 1-5; ver
-- Alianzagrafica.Evaluacion180.Web.Models.Entidades.RespuestaEvaluacion.RespuestaDetalle.Calificacion.
CREATE TABLE dbo.RespuestaDetalle (
    IdRespuesta   INT           NOT NULL,
    IdCompetencia INT           NOT NULL,
    Calificacion  DECIMAL(5,2)  NOT NULL,
    Comentario    NVARCHAR(500) NULL,
    CONSTRAINT PK_RespuestaDetalle PRIMARY KEY (IdRespuesta, IdCompetencia),
    CONSTRAINT FK_Detalle_Respuesta FOREIGN KEY (IdRespuesta) REFERENCES dbo.RespuestaEvaluacion (IdRespuesta),
    CONSTRAINT FK_Detalle_Competencia FOREIGN KEY (IdCompetencia) REFERENCES dbo.Competencia (IdCompetencia),
    CONSTRAINT CK_Detalle_Calificacion CHECK (Calificacion BETWEEN 0 AND 100)
);
GO

-- Entregable 13: calificación del evaluador para un comportamiento individual (columna "NOTA
-- INDIVIDUAL" del Excel origen "EVALUACION DESEMPEÑO_Evaluaciones"). El promedio de estas filas,
-- agrupadas por competencia, es dbo.RespuestaDetalle.Calificacion (columna "NOTA FINAL" del
-- Excel, fórmula =AVERAGE(...) por competencia).
CREATE TABLE dbo.RespuestaComportamientoDetalle (
    IdRespuesta      INT           NOT NULL,
    IdComportamiento INT           NOT NULL,
    Calificacion     DECIMAL(5,2)  NOT NULL,
    CONSTRAINT PK_RespuestaComportamientoDetalle PRIMARY KEY (IdRespuesta, IdComportamiento),
    CONSTRAINT FK_DetalleComp_Respuesta FOREIGN KEY (IdRespuesta) REFERENCES dbo.RespuestaEvaluacion (IdRespuesta),
    CONSTRAINT FK_DetalleComp_Comportamiento FOREIGN KEY (IdComportamiento) REFERENCES dbo.Comportamiento (IdComportamiento),
    CONSTRAINT CK_DetalleComp_Calificacion CHECK (Calificacion BETWEEN 0 AND 100)
);
GO

-- Entregable 11: respuesta de un indicador de gestión — Meta y Resultado del mes en %, igual que
-- Calificacion arriba (ver dbo.IndicadorGestion). Desde el Entregable 12, Meta aquí es solo una
-- FOTO/snapshot del valor fijo del catálogo (dbo.IndicadorGestion.Meta) tomado al guardar la
-- respuesta — el evaluador ya no la escribe, solo diligencia ResultadoMes.
CREATE TABLE dbo.RespuestaIndicadorDetalle (
    IdRespuesta  INT           NOT NULL,
    IdIndicador  INT           NOT NULL,
    Meta         DECIMAL(6,2)  NULL,
    ResultadoMes DECIMAL(6,2)  NULL,
    CONSTRAINT PK_RespuestaIndicadorDetalle PRIMARY KEY (IdRespuesta, IdIndicador),
    CONSTRAINT FK_DetalleInd_Respuesta FOREIGN KEY (IdRespuesta) REFERENCES dbo.RespuestaEvaluacion (IdRespuesta),
    CONSTRAINT FK_DetalleInd_Indicador FOREIGN KEY (IdIndicador) REFERENCES dbo.IndicadorGestion (IdIndicador)
);
GO

-- ---- Resultado consolidado por evaluado y periodo ----
-- Promedio* en DECIMAL(5,2): antes DECIMAL(4,2) alcanzaba porque la escala tope era 5.00; desde
-- el Entregable 12 la escala nativa es 0-100.00 y necesita un dígito entero más.
CREATE TABLE dbo.ResultadoConsolidado (
    CodigoEvaluado          INT           NOT NULL,
    IdPeriodo               INT           NOT NULL,
    PromedioAutoevaluacion  DECIMAL(5,2)  NULL,
    PromedioJefe            DECIMAL(5,2)  NULL,
    PromedioAscendente      DECIMAL(5,2)  NULL,
    PromedioGeneral         DECIMAL(5,2)  NULL,
    FechaConsolidacion      DATETIME2(0)  NULL,
    CONSTRAINT PK_ResultadoConsolidado PRIMARY KEY (CodigoEvaluado, IdPeriodo),
    CONSTRAINT FK_Resultado_Empleado FOREIGN KEY (CodigoEvaluado) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT FK_Resultado_Periodo FOREIGN KEY (IdPeriodo) REFERENCES dbo.PeriodoEvaluacion (IdPeriodo)
);
GO

-- ---- Auditoría ----
CREATE TABLE dbo.Auditoria (
    IdEvento    BIGINT IDENTITY(1,1)  NOT NULL,
    IdUsuario   INT                   NULL,
    TipoEvento  VARCHAR(50)           NOT NULL,
    Detalle     NVARCHAR(500)         NULL,
    FechaHora   DATETIME2(0)          NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT (SYSDATETIME()),
    DireccionIP VARCHAR(45)           NULL,
    CONSTRAINT PK_Auditoria PRIMARY KEY (IdEvento),
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (IdUsuario) REFERENCES dbo.Usuario (IdUsuario)
);
GO

-- ---- Módulo de envío de resultados por correo y WhatsApp (RF-23) ----
-- ContactoNotificacion: número de WhatsApp del empleado para el envío del resumen de
-- resultados. Se guarda en una tabla SEPARADA de dbo.Empleado a propósito: Empleado es un
-- espejo de solo lectura de Novasoft (sección 3.2 del documento de diseño; el sistema de
-- evaluación NUNCA edita datos maestros de empleados), así que este dato local sobrevive a
-- cada resincronización desde Novasoft sin violar ese principio.
CREATE TABLE dbo.ContactoNotificacion (
    CodigoEmpleado      INT           NOT NULL,
    TelefonoWhatsApp    VARCHAR(30)   NULL,
    FechaActualizacion  DATETIME2(0)  NOT NULL,
    CONSTRAINT PK_ContactoNotificacion PRIMARY KEY (CodigoEmpleado),
    CONSTRAINT FK_ContactoNotificacion_Empleado FOREIGN KEY (CodigoEmpleado) REFERENCES dbo.Empleado (CodigoEmpleado)
);
GO

-- EnvioResultadoToken: enlace público temporal de la imagen-resumen que el proveedor de
-- WhatsApp (Twilio) descarga al enviar el mensaje. Token aleatorio de un solo uso conceptual,
-- con vigencia corta (ver EnvioResultadoService.VigenciaToken en el código, 30 minutos).
-- Pendiente de mejora documentado: no hay todavía una tarea programada que purgue filas ya
-- expiradas de esta tabla.
CREATE TABLE dbo.EnvioResultadoToken (
    Token            CHAR(32)       NOT NULL,
    CodigoEvaluado   INT            NOT NULL,
    IdPeriodo        INT            NOT NULL,
    ImagenPng        VARBINARY(MAX) NOT NULL,
    FechaCreacion    DATETIME2(0)   NOT NULL,
    FechaExpiracion  DATETIME2(0)   NOT NULL,
    CONSTRAINT PK_EnvioResultadoToken PRIMARY KEY (Token),
    CONSTRAINT FK_EnvioResultadoToken_Empleado FOREIGN KEY (CodigoEvaluado) REFERENCES dbo.Empleado (CodigoEmpleado),
    CONSTRAINT FK_EnvioResultadoToken_Periodo FOREIGN KEY (IdPeriodo) REFERENCES dbo.PeriodoEvaluacion (IdPeriodo)
);
GO

-- =========================================================
-- 2. CATÁLOGOS BASE
-- =========================================================

INSERT INTO dbo.TipoPersonal (Nombre, PermiteEvaluacionAscendente) VALUES
    (N'Directivo',           1),
    (N'Mando medio',         1),
    (N'Administrativo',      0),
    (N'Operario',            0),
    (N'Auxiliar de planta',  0),
    (N'Conductor',           0);
GO

INSERT INTO dbo.Rol (NombreRol, Descripcion) VALUES
    (N'Administrador del sistema',        N'Perfil de TI: configuración general, usuarios, roles, monitoreo de integración con Novasoft, auditoría.'),
    (N'Administrador de Gestión Humana',  N'Perfil de RRHH: crea/abre/cierra periodos, define competencias y formularios, ajusta asignaciones, genera reportes globales.'),
    (N'Jefe / Evaluador',                 N'Todo colaborador con personal a cargo: diligencia evaluaciones de sus colaboradores directos.'),
    (N'Colaborador / Evaluado',           N'Todo empleado activo: diligencia su autoevaluación y su evaluación ascendente si aplica.'),
    (N'Consulta directiva',               N'Alta dirección: consulta reportes y tableros consolidados, sin editar.');
GO

-- =========================================================
-- 3. EMPLEADOS (FICTICIOS) — sustituto temporal de Novasoft
--    Estructura organizacional de ejemplo para una empresa
--    del sector gráfico industrial.
-- =========================================================

INSERT INTO dbo.Empleado
    (CodigoEmpleado, NumeroIdentificacion, Nombre, Cargo, Area, IdTipoPersonal, CodigoJefeDirecto, CorreoElectronico, Estado, FechaIngreso)
VALUES
    -- Directivos
    (1001, '10000001', N'Ana María Duarte',        N'Gerente General',                    N'Gerencia General', 1, NULL, 'ana.duarte@alianzagrafica.com',       'Activo', '2014-03-03'),
    (1002, '10000002', N'Carlos Eduardo Salazar',   N'Gerente de Producción',              N'Producción',       1, 1001, 'carlos.salazar@alianzagrafica.com',   'Activo', '2015-06-16'),
    (1003, '10000003', N'Diana Patricia Gómez',     N'Gerente Comercial y Financiero',     N'Comercial',        1, 1001, 'diana.gomez@alianzagrafica.com',      'Activo', '2016-01-11'),
    (1023, '10000023', N'Rodrigo Rodríguez Vivas',  N'Director de Tecnología',             N'Tecnología',       1, 1001, 'director.tecnologia@alianzagrafica.com', 'Activo', '2020-02-10'),

    -- Mandos medios
    (1004, '10000004', N'Jorge Luis Martínez',      N'Jefe de Recursos Humanos',           N'Gestión Humana',   2, 1001, 'jorge.martinez@alianzagrafica.com',   'Activo', '2017-04-24'),
    (1005, '10000005', N'Laura Fernanda Rojas',     N'Jefe de Producción — Turno 1',       N'Producción',       2, 1002, 'laura.rojas@alianzagrafica.com',      'Activo', '2016-09-19'),
    (1006, '10000006', N'Andrés Felipe Castaño',    N'Coordinador de Calidad',             N'Calidad',          2, 1002, 'andres.castano@alianzagrafica.com',   'Activo', '2018-02-05'),
    (1007, '10000007', N'Mónica Alexandra Torres',  N'Jefe de Contabilidad y Cartera',     N'Contabilidad',     2, 1003, 'monica.torres@alianzagrafica.com',    'Activo', '2015-11-30'),

    -- Administrativos
    (1008, '10000008', N'Sandra Milena Pérez',      N'Analista de Nómina',                 N'Gestión Humana',   3, 1004, 'sandra.perez@alianzagrafica.com',     'Activo', '2019-05-13'),
    (1009, '10000009', N'Julián David Herrera',     N'Asistente Contable',                 N'Contabilidad',     3, 1007, 'julian.herrera@alianzagrafica.com',   'Activo', '2020-08-24'),
    (1010, '10000010', N'Paola Andrea Ríos',        N'Analista Comercial',                 N'Comercial',        3, 1003, 'paola.rios@alianzagrafica.com',       'Activo', '2018-10-01'),
    (1011, '10000011', N'Camilo Ernesto Vargas',    N'Asistente de Compras',               N'Compras',          3, 1003, 'camilo.vargas@alianzagrafica.com',    'Activo', '2021-03-08'),
    (1012, '10000012', N'Natalia Ximena Bermúdez',  N'Auxiliar Administrativa',            N'Gestión Humana',   3, 1004, 'natalia.bermudez@alianzagrafica.com', 'Activo', '2022-01-17'),

    -- Operarios
    (1013, '10000013', N'Wilson Alberto Gómez',     N'Operario de Impresión Offset',       N'Producción',       4, 1005, 'wilson.gomez@alianzagrafica.com',     'Activo', '2017-07-10'),
    (1014, '10000014', N'Yesenia Marcela Ospina',   N'Operario de Troquelado',             N'Producción',       4, 1005, 'yesenia.ospina@alianzagrafica.com',   'Activo', '2019-02-22'),
    (1015, '10000015', N'Édgar Iván Ramírez',       N'Operario de Encuadernación',         N'Producción',       4, 1005, 'edgar.ramirez@alianzagrafica.com',    'Activo', '2018-06-04'),
    (1016, '10000016', N'Diana Carolina Muñoz',     N'Operario de Guillotina',             N'Producción',       4, 1005, 'diana.munoz@alianzagrafica.com',      'Activo', '2020-11-16'),
    (1017, '10000017', N'Fabián Steven Cárdenas',   N'Operario de Preprensa Digital',      N'Producción',       4, 1005, 'fabian.cardenas@alianzagrafica.com',  'Activo', '2021-09-27'),
    (1018, '10000018', N'Luz Dary Cortés',          N'Operario de Control de Calidad',     N'Calidad',          4, 1006, 'luz.cortes@alianzagrafica.com',       'Activo', '2019-12-02'),

    -- Auxiliares de planta
    (1019, '10000019', N'Héctor Julio Peña',        N'Auxiliar de Logística e Insumos',    N'Producción',       5, 1005, 'hector.pena@alianzagrafica.com',      'Activo', '2021-04-19'),
    (1020, '10000020', N'Marlon Andrés Quintero',   N'Auxiliar de Alistamiento',           N'Producción',       5, 1005, 'marlon.quintero@alianzagrafica.com',  'Activo', '2022-06-06'),
    (1021, '10000021', N'Rocío del Pilar Sánchez',  N'Auxiliar de Aseo Industrial',        N'Planta General',   5, 1006, 'rocio.sanchez@alianzagrafica.com',    'Activo', '2020-01-20'),
    (1022, '10000022', N'Kevin Santiago Bautista',  N'Auxiliar de Empaque y Despacho',     N'Producción',       5, 1005, 'kevin.bautista@alianzagrafica.com',   'Activo', '2023-02-14'),

    -- Conductor (rol de despachos/transporte — mismo tipo de personal y el mismo colaborador
    -- ficticio de ejemplo usado en la demo, Entregable 5, para que ambos entornos sean
    -- consistentes; ver dbo.TipoPersonal, IdTipoPersonal = 6)
    (1024, '10000024', N'Diego Alejandro Salazar',  N'Conductor de Despachos',             N'Logística',        6, 1005, 'diego.salazar@alianzagrafica.com',    'Activo', '2022-09-05');
GO

-- Contacto de WhatsApp de ejemplo (RF-23) para un subconjunto de empleados — números
-- ficticios, para poder probar el envío del resumen de resultados sin depender de Novasoft.
-- No es obligatorio que todos los empleados tengan un número: los que no lo tengan
-- simplemente no reciben el resumen por ese canal (solo por correo).
INSERT INTO dbo.ContactoNotificacion (CodigoEmpleado, TelefonoWhatsApp, FechaActualizacion) VALUES
    (1001, '3000000001', SYSUTCDATETIME()),
    (1002, '3000000002', SYSUTCDATETIME()),
    (1004, '3000000004', SYSUTCDATETIME()),
    (1005, '3000000005', SYSUTCDATETIME()),
    (1013, '3000000013', SYSUTCDATETIME()),
    (1023, '3000000023', SYSUTCDATETIME());
GO

-- =========================================================
-- 4. USUARIOS Y ROLES
-- =========================================================

INSERT INTO dbo.Usuario (CodigoEmpleado, NombreUsuario, TipoAutenticacion, Activo)
SELECT CodigoEmpleado, CorreoElectronico, 'ActiveDirectory', 1
FROM dbo.Empleado;
GO

-- Todo empleado activo es, como mínimo, "Colaborador / Evaluado"
INSERT INTO dbo.UsuarioRol (IdUsuario, IdRol)
SELECT u.IdUsuario, r.IdRol
FROM dbo.Usuario u
JOIN dbo.Rol r ON r.NombreRol = N'Colaborador / Evaluado';
GO

-- Todo empleado que aparece como jefe directo de alguien recibe además el rol "Jefe / Evaluador"
INSERT INTO dbo.UsuarioRol (IdUsuario, IdRol)
SELECT DISTINCT u.IdUsuario, r.IdRol
FROM dbo.Empleado jefe
JOIN dbo.Usuario u ON u.CodigoEmpleado = jefe.CodigoEmpleado
JOIN dbo.Rol r ON r.NombreRol = N'Jefe / Evaluador'
WHERE jefe.CodigoEmpleado IN (SELECT CodigoJefeDirecto FROM dbo.Empleado WHERE CodigoJefeDirecto IS NOT NULL);
GO

-- Rol de Gestión Humana para el Jefe de Recursos Humanos
INSERT INTO dbo.UsuarioRol (IdUsuario, IdRol)
SELECT u.IdUsuario, r.IdRol
FROM dbo.Usuario u
JOIN dbo.Rol r ON r.NombreRol = N'Administrador de Gestión Humana'
WHERE u.CodigoEmpleado = 1004;
GO

-- Rol de administrador del sistema para el Director de Tecnología
INSERT INTO dbo.UsuarioRol (IdUsuario, IdRol)
SELECT u.IdUsuario, r.IdRol
FROM dbo.Usuario u
JOIN dbo.Rol r ON r.NombreRol = N'Administrador del sistema'
WHERE u.CodigoEmpleado = 1023;
GO

-- Rol de consulta directiva para la Gerencia General
INSERT INTO dbo.UsuarioRol (IdUsuario, IdRol)
SELECT u.IdUsuario, r.IdRol
FROM dbo.Usuario u
JOIN dbo.Rol r ON r.NombreRol = N'Consulta directiva'
WHERE u.CodigoEmpleado = 1001;
GO

-- =========================================================
-- 5. COMPETENCIAS Y COMPORTAMIENTOS (Entregable 13)
-- =========================================================

-- Reemplaza el catálogo anterior (competencias específicas por tipo de personal — "Visión
-- estratégica" del Directivo, "Liderazgo de equipos" del Mando medio, las 6 del Conductor, etc.)
-- por el del formato real "EVALUACION DESEMPEÑO_Evaluaciones" de Alianzagrafica (Excel
-- adjuntado por el usuario), que desglosa cada competencia en sus comportamientos observables
-- (columna "COMPORTAMIENTOS"). A pedido explícito del usuario ("mismo listado para todos los
-- perfiles"), es un catálogo ÚNICO y genérico (IdTipoPersonal = NULL): las mismas 6 competencias
-- (3 'Organizacional' + 3 'DeRol') y sus 20 comportamientos aplican igual a los seis tipos de
-- personal. Categoria conserva el mismo significado que antes: 'Organizacional' (macro-grupo
-- "EVALUACION DE COMPETENCIAS ORGANIZACIONALES", 20% del total) y 'DeRol' (macro-grupo
-- "EVALUACION DE COMPETENCIAS DE ROL", 30% del total) — ver sección 6 y Constantes.cs.
--
-- La "NOTA FINAL" de cada competencia (dbo.RespuestaDetalle.Calificacion) es el promedio de sus
-- comportamientos ya calificados — se calcula en el servidor (nunca a partir de un total posteado
-- directamente) al guardar dbo.RespuestaComportamientoDetalle — ver sección 8 más abajo para un
-- ejemplo end-to-end.
INSERT INTO dbo.Competencia (Nombre, Descripcion, IdTipoPersonal, Categoria) VALUES
    (N'Adherencia a normas y políticas organizacionales',
     N'Capacidad para adaptarse a las normas y políticas de la organización, mostrando compromiso al conocerlas, entenderlas y aplicarlas.',
     NULL, N'Organizacional'),
    (N'Compromiso con la calidad de trabajo',
     N'Capacidad para actuar con minuciosidad, velocidad y sentido de urgencia y tomar decisiones para alcanzar los objetivos de su puesto de trabajo, del área, u organizacionales, con altos niveles de desempeño.',
     NULL, N'Organizacional'),
    (N'Eficiencia y Productividad',
     N'Habilidad para dirigir las propias acciones y/o las de otros de forma que agreguen valor a la organización, alcanzando los objetivos, cumpliendo con el tiempo disponible y con la calidad requerida.',
     NULL, N'Organizacional'),
    (N'Atención al detalle',
     N'Capacidad para identificar, evaluar y controlar los detalles que comprende una acción o actividad, verificando la calidad y el procedimiento, para evitar afectaciones en la gestión.',
     NULL, N'DeRol'),
    (N'Calidad de trabajo',
     N'Capacidad para determinar eficazmente las metas y prioridades de su tarea/área/proyecto estipulando la acción, los plazos y los recursos requeridos.',
     NULL, N'DeRol'),
    (N'Planificación y seguimiento',
     N'Es la capacidad de identificar y determinar de forma efectiva sus prioridades estableciendo fechas, actividades y responsables.',
     NULL, N'DeRol');
GO

-- Comportamientos observables de cada competencia (columna "COMPORTAMIENTOS" del Excel origen),
-- en el mismo orden en que aparecen sus filas. 20 comportamientos en total: 12 en el grupo
-- Organizacional (6+3+3) y 8 en el grupo DeRol (5+1+2).
INSERT INTO dbo.Comportamiento (IdCompetencia, Descripcion, Orden)
SELECT c.IdCompetencia, v.Descripcion, v.Orden
FROM (VALUES
    -- Adherencia a normas y políticas organizacionales
    (N'Adherencia a normas y políticas organizacionales', 1, N'Cumple con las normas y procedimientos establecidos por la compañía.'),
    (N'Adherencia a normas y políticas organizacionales', 2, N'Utiliza los elementos de protección personal.'),
    (N'Adherencia a normas y políticas organizacionales', 3, N'Porta el uniforme adecuadamente, conforme a las políticas de la compañía.'),
    (N'Adherencia a normas y políticas organizacionales', 4, N'Se dirige con respeto frente a su jefe y compañeros.'),
    (N'Adherencia a normas y políticas organizacionales', 5, N'Cuenta con disposición para el trabajo adicional cuando la compañía lo requiere.'),
    (N'Adherencia a normas y políticas organizacionales', 6, N'Cumple con los horarios establecidos para su turno de trabajo.'),
    -- Compromiso con la calidad de trabajo
    (N'Compromiso con la calidad de trabajo', 1, N'Utiliza métodos estructurados para definir las actividades necesarias durante el proceso, para lograr el resultado esperado (producto).'),
    (N'Compromiso con la calidad de trabajo', 2, N'Evalúa los posibles riesgos, consecuencias e impactos negativos que se pueden obtener como consecuencia de la falta de control de proceso.'),
    (N'Compromiso con la calidad de trabajo', 3, N'Toma decisiones y emprende acciones de mejora en base al análisis de los resultados obtenidos.'),
    -- Eficiencia y Productividad
    (N'Eficiencia y Productividad', 1, N'Mantiene un buen nivel de actividad, variando su ritmo en función del tiempo disponible y realizando su trabajo según los tiempos establecidos.'),
    (N'Eficiencia y Productividad', 2, N'Se esfuerza por aumentar el volumen de trabajo realizado, sin descuidar la calidad.'),
    (N'Eficiencia y Productividad', 3, N'Comprueba que la calidad y los beneficios obtenidos de su trabajo son los esperados.'),
    -- Atención al detalle
    (N'Atención al detalle', 1, N'Lee e interpreta la orden de producción.'),
    (N'Atención al detalle', 2, N'Realiza un adecuado despeje de línea al iniciar la inspección de cada producto.'),
    (N'Atención al detalle', 3, N'Empaca los productos conforme a las especificaciones de la orden de producción o manual de empaque.'),
    (N'Atención al detalle', 4, N'Realiza el control de cierre al finalizar la revisión de la orden de producción.'),
    (N'Atención al detalle', 5, N'Evita mezclas de producto en todas las referencias procesadas.'),
    -- Calidad de trabajo (grupo DeRol)
    (N'Calidad de trabajo', 1, N'Informa las no conformidades observadas durante la realización de procesos de inspección.'),
    -- Planificación y seguimiento
    (N'Planificación y seguimiento', 1, N'Marca las fajillas y paquetes con el número que le corresponde.'),
    (N'Planificación y seguimiento', 2, N'Revisa y segrega eficientemente cada producto inspeccionado, ya sea lateral y AUT, en mesa o plano.')
) AS v(CompetenciaNombre, Orden, Descripcion)
JOIN dbo.Competencia c ON c.Nombre = v.CompetenciaNombre AND c.IdTipoPersonal IS NULL;
GO

-- Indicadores de gestión (Entregable 11 — macro-grupo "INDICADORES DE GESTIÓN", peso 50% del
-- total, según el formato real "EVALUACION DESEMPEÑO Indicadores" de Alianzagrafica). Genéricos
-- (IdTipoPersonal = NULL, aplican a todo el personal), igual que en DemoSeed.cs (aplicación).
-- Las ponderaciones (33.33% cada uno) están tal como figuran en el Excel origen y, junto con las
-- otras tres filas, suman ~133.33% dentro del catálogo (no 100%) — se mantienen así porque
-- reflejan el peso RELATIVO entre los cuatro indicadores tal como viene del Excel. Desde el
-- Entregable 14, al generar cada formulario esa Ponderacion relativa se normaliza para que el
-- grupo "Indicadores de Gestión" siempre sume exactamente su peso nominal fijo (50% del total) —
-- ver Constantes.PesoIndicadoresGestion y la nota en la sección 6 de este script.
-- Meta (Entregable 12 — a pedido explícito del usuario, valores fijos): Ausentismo = 90,
-- Calidad = 100, "Cultura: 5S+1" = 90, Eficiencia = 90. Igual que DemoSeed.cs (aplicación).
INSERT INTO dbo.IndicadorGestion (Nombre, Formula, Ponderacion, Meta, IdTipoPersonal) VALUES
    (N'Cultura: 5S+1', N'Costo de reclamos del cliente ($) facturación.', 33.33, 90.00, NULL),
    (N'Eficiencia', N'Cantidad unidades defectuosas / Cantidad unidades producidas', 33.33, 90.00, NULL),
    (N'Calidad', N'(Horas laboradas - Horas de ausentismo) / Horas totales laboradas', 33.33, 100.00, NULL),
    (N'Ausentismo', N'Rendimiento real / Rendimiento esperado', 33.33, 90.00, NULL);
GO

-- =========================================================
-- 6. PERIODO DE EVALUACIÓN 2026 + FORMULARIOS
-- =========================================================

INSERT INTO dbo.PeriodoEvaluacion (Nombre, FechaApertura, FechaCierre, Estado)
VALUES (N'Evaluación de Desempeño 2026', '2026-09-01', '2026-10-15', 'Abierto');
GO

-- Un formulario de Autoevaluación y uno de Jefe para cada tipo de personal,
-- y adicionalmente uno de Ascendente para Directivo y Mando medio
-- (según la tabla de la sección 5.1 del documento de diseño).
INSERT INTO dbo.FormularioEvaluacion (IdPeriodo, TipoRelacion, IdTipoPersonal, Nombre)
SELECT p.IdPeriodo, rel.TipoRelacion, tp.IdTipoPersonal,
       CONCAT(N'Formulario ', rel.TipoRelacion, N' — ', tp.Nombre, N' — ', p.Nombre)
FROM dbo.PeriodoEvaluacion p
CROSS JOIN dbo.TipoPersonal tp
CROSS JOIN (VALUES ('Autoevaluacion'), ('Jefe')) AS rel(TipoRelacion)
WHERE p.Nombre = N'Evaluación de Desempeño 2026'
UNION ALL
SELECT p.IdPeriodo, 'Ascendente', tp.IdTipoPersonal,
       CONCAT(N'Formulario Ascendente — ', tp.Nombre, N' — ', p.Nombre)
FROM dbo.PeriodoEvaluacion p
CROSS JOIN dbo.TipoPersonal tp
WHERE p.Nombre = N'Evaluación de Desempeño 2026'
  AND tp.PermiteEvaluacionAscendente = 1;
GO

-- Ponderación (RF-06/RF-07 ampliado en el Entregable 11): cada formulario incluye las
-- competencias genéricas (Categoria = 'Organizacional') más las específicas del tipo de personal
-- (Categoria = 'DeRol'), y además todos los indicadores de gestión activos aplicables (macro-
-- grupo 'IndicadoresGestion', formato real "EVALUACION DESEMPEÑO Indicadores"). Si una
-- competencia específica tiene el mismo nombre que una genérica, la específica la reemplaza (ej.
-- "Trabajo en equipo" del Conductor). Cuando las TRES claves de macro-grupo conocidas están
-- presentes en el formulario (Organizacional + DeRol + IndicadoresGestion, que es el caso de
-- todos los formularios de esta demo porque los 4 indicadores son genéricos), se usan los pesos
-- FIJOS del Entregable 11: IndicadoresGestion 50%, DeRol 30%, Organizacional 20%. En cualquier
-- otro caso (por compatibilidad hacia atrás, ej. si en el futuro se desactivan todos los
-- indicadores) se conserva el reparto parejo histórico 100%/N entre las claves presentes. Dentro
-- de "Organizacional"/"DeRol" el peso del grupo se reparte en partes iguales entre sus
-- competencias; dentro de "IndicadoresGestion" cada indicador recibe su propia Ponderacion
-- configurada (no reparto parejo — ver el segundo INSERT más abajo). Misma lógica que
-- AsignacionService.GenerarFormulariosAsync en la aplicación — mantener sincronizadas.
DECLARE @PesoPorGrupo TABLE (
    IdFormulario INT          NOT NULL,
    ClaveGrupo   NVARCHAR(50) NOT NULL,
    PesoGrupo    DECIMAL(9,4) NOT NULL,
    PRIMARY KEY (IdFormulario, ClaveGrupo)
);

;WITH CompetenciaFormulario AS (
    SELECT
        f.IdFormulario,
        c.IdCompetencia,
        c.Categoria,
        ROW_NUMBER() OVER (
            PARTITION BY f.IdFormulario, c.Nombre
            ORDER BY CASE WHEN c.IdTipoPersonal IS NULL THEN 1 ELSE 0 END
        ) AS Prioridad
    FROM dbo.FormularioEvaluacion f
    JOIN dbo.Competencia c
        ON c.Activa = 1
       AND (c.IdTipoPersonal = f.IdTipoPersonal OR c.IdTipoPersonal IS NULL)
),
ClaveCompetencia AS (
    SELECT DISTINCT IdFormulario,
           COALESCE(Categoria, CONCAT(N'__sin_categoria_', IdCompetencia)) AS ClaveGrupo
    FROM CompetenciaFormulario
    WHERE Prioridad = 1
),
ClaveIndicador AS (
    SELECT DISTINCT f.IdFormulario, N'IndicadoresGestion' AS ClaveGrupo
    FROM dbo.FormularioEvaluacion f
    JOIN dbo.IndicadorGestion i
        ON i.Activa = 1
       AND (i.IdTipoPersonal = f.IdTipoPersonal OR i.IdTipoPersonal IS NULL)
),
ClavesPresentes AS (
    SELECT IdFormulario, ClaveGrupo FROM ClaveCompetencia
    UNION
    SELECT IdFormulario, ClaveGrupo FROM ClaveIndicador
),
ConteoClaves AS (
    SELECT IdFormulario, COUNT(*) AS TotalClaves
    FROM ClavesPresentes
    GROUP BY IdFormulario
),
UsaPesosFijos AS (
    -- Formularios donde están presentes EXACTAMENTE las tres claves conocidas (ni de más ni de
    -- menos) — mismo umbral que AsignacionService.GenerarFormulariosAsync (usaPesosFijos).
    SELECT cp.IdFormulario
    FROM ClavesPresentes cp
    JOIN ConteoClaves cc ON cc.IdFormulario = cp.IdFormulario
    WHERE cp.ClaveGrupo IN (N'Organizacional', N'DeRol', N'IndicadoresGestion')
    GROUP BY cp.IdFormulario
    HAVING COUNT(DISTINCT cp.ClaveGrupo) = 3 AND MIN(cc.TotalClaves) = 3
)
INSERT INTO @PesoPorGrupo (IdFormulario, ClaveGrupo, PesoGrupo)
SELECT cp.IdFormulario, cp.ClaveGrupo,
       CASE
           WHEN upf.IdFormulario IS NOT NULL THEN
               CASE cp.ClaveGrupo
                   WHEN N'IndicadoresGestion' THEN 50.0
                   WHEN N'Organizacional'     THEN 20.0
                   WHEN N'DeRol'              THEN 30.0
               END
           ELSE 100.0 / cc.TotalClaves
       END
FROM ClavesPresentes cp
JOIN ConteoClaves cc ON cc.IdFormulario = cp.IdFormulario
LEFT JOIN UsaPesosFijos upf ON upf.IdFormulario = cp.IdFormulario;

;WITH CompetenciaFormulario AS (
    SELECT
        f.IdFormulario,
        c.IdCompetencia,
        c.Nombre,
        c.Categoria,
        ROW_NUMBER() OVER (
            PARTITION BY f.IdFormulario, c.Nombre
            ORDER BY CASE WHEN c.IdTipoPersonal IS NULL THEN 1 ELSE 0 END
        ) AS Prioridad
    FROM dbo.FormularioEvaluacion f
    JOIN dbo.Competencia c
        ON c.Activa = 1
       AND (c.IdTipoPersonal = f.IdTipoPersonal OR c.IdTipoPersonal IS NULL)
),
CompetenciaElegida AS (
    SELECT IdFormulario, IdCompetencia,
           COALESCE(Categoria, CONCAT(N'__sin_categoria_', IdCompetencia)) AS ClaveGrupo
    FROM CompetenciaFormulario
    WHERE Prioridad = 1
),
CompetenciasPorGrupo AS (
    SELECT IdFormulario, ClaveGrupo, COUNT(*) AS CompetenciasEnGrupo
    FROM CompetenciaElegida
    GROUP BY IdFormulario, ClaveGrupo
)
INSERT INTO dbo.FormularioCompetencia (IdFormulario, IdCompetencia, Ponderacion)
SELECT ce.IdFormulario, ce.IdCompetencia,
       CAST(pg.PesoGrupo / cg.CompetenciasEnGrupo AS DECIMAL(5,2))
FROM CompetenciaElegida ce
JOIN CompetenciasPorGrupo cg ON cg.IdFormulario = ce.IdFormulario AND cg.ClaveGrupo = ce.ClaveGrupo
JOIN @PesoPorGrupo pg ON pg.IdFormulario = ce.IdFormulario AND pg.ClaveGrupo = ce.ClaveGrupo;

-- Indicadores de gestión: cada indicador recibe el peso ABSOLUTO resultante de aplicar su propia
-- Ponderacion (% dentro del grupo, ej. 33.33) al peso del grupo 'IndicadoresGestion' — NO reparto
-- parejo entre indicadores, a diferencia de las competencias. Hasta el Entregable 13, ese % se
-- aplicaba directamente sobre 100 (Ponderacion/100.0) sin normalizar: como las 4 Ponderacion del
-- catálogo suman ~133.33% (no 100%), el peso EFECTIVO del grupo dentro de la nota final terminaba
-- siendo distinto de su peso nominal fijo (50%) — el usuario detectó que los tres macro-grupos no
-- quedaban bien ponderados (Indicadores no sumaba realmente 50%). Corregido (Entregable 14)
-- dividiendo por la suma REAL de las Ponderacion de los indicadores presentes en el formulario en
-- vez de por 100 fijo, para que el grupo siempre sume exactamente su peso nominal, preservando el
-- peso relativo entre indicadores. Igual que AsignacionService.GenerarFormulariosAsync
-- (pesoAbsoluto = pesoGrupoIndicadores * Ponderacion/sumaPonderacionIndicadores).
INSERT INTO dbo.FormularioIndicador (IdFormulario, IdIndicador, Ponderacion)
SELECT f.IdFormulario, i.IdIndicador,
       CAST(pg.PesoGrupo * (i.Ponderacion / spi.SumaPonderacion) AS DECIMAL(5,2))
FROM dbo.FormularioEvaluacion f
JOIN dbo.IndicadorGestion i
    ON i.Activa = 1
   AND (i.IdTipoPersonal = f.IdTipoPersonal OR i.IdTipoPersonal IS NULL)
JOIN @PesoPorGrupo pg ON pg.IdFormulario = f.IdFormulario AND pg.ClaveGrupo = N'IndicadoresGestion'
CROSS APPLY (
    SELECT SUM(i2.Ponderacion) AS SumaPonderacion
    FROM dbo.IndicadorGestion i2
    WHERE i2.Activa = 1
      AND (i2.IdTipoPersonal = f.IdTipoPersonal OR i2.IdTipoPersonal IS NULL)
) spi
WHERE spi.SumaPonderacion > 0;
GO

-- =========================================================
-- 7. GENERACIÓN AUTOMÁTICA DE ASIGNACIONES EVALUADOR-EVALUADO
--    (materializa RF-09 a partir de la jerarquía de dbo.Empleado)
-- =========================================================

DECLARE @IdPeriodoActual INT = (SELECT IdPeriodo FROM dbo.PeriodoEvaluacion WHERE Nombre = N'Evaluación de Desempeño 2026');

-- 7.1 Autoevaluación: todo empleado activo se autoevalúa
INSERT INTO dbo.AsignacionEvaluacion (IdPeriodo, CodigoEvaluador, CodigoEvaluado, TipoRelacion, IdFormulario)
SELECT @IdPeriodoActual, e.CodigoEmpleado, e.CodigoEmpleado, 'Autoevaluacion', f.IdFormulario
FROM dbo.Empleado e
JOIN dbo.FormularioEvaluacion f
    ON f.IdPeriodo = @IdPeriodoActual
   AND f.TipoRelacion = 'Autoevaluacion'
   AND f.IdTipoPersonal = e.IdTipoPersonal
WHERE e.Estado = 'Activo';

-- 7.2 Jefe -> Colaborador: el jefe directo evalúa a cada colaborador
INSERT INTO dbo.AsignacionEvaluacion (IdPeriodo, CodigoEvaluador, CodigoEvaluado, TipoRelacion, IdFormulario)
SELECT @IdPeriodoActual, e.CodigoJefeDirecto, e.CodigoEmpleado, 'Jefe', f.IdFormulario
FROM dbo.Empleado e
JOIN dbo.FormularioEvaluacion f
    ON f.IdPeriodo = @IdPeriodoActual
   AND f.TipoRelacion = 'Jefe'
   AND f.IdTipoPersonal = e.IdTipoPersonal
WHERE e.Estado = 'Activo'
  AND e.CodigoJefeDirecto IS NOT NULL;

-- 7.3 Ascendente: el colaborador evalúa a su jefe, solo si el tipo de
--     personal del jefe tiene habilitada la evaluación ascendente
INSERT INTO dbo.AsignacionEvaluacion (IdPeriodo, CodigoEvaluador, CodigoEvaluado, TipoRelacion, IdFormulario)
SELECT @IdPeriodoActual, e.CodigoEmpleado, jefe.CodigoEmpleado, 'Ascendente', f.IdFormulario
FROM dbo.Empleado e
JOIN dbo.Empleado jefe ON jefe.CodigoEmpleado = e.CodigoJefeDirecto
JOIN dbo.TipoPersonal tpJefe ON tpJefe.IdTipoPersonal = jefe.IdTipoPersonal
JOIN dbo.FormularioEvaluacion f
    ON f.IdPeriodo = @IdPeriodoActual
   AND f.TipoRelacion = 'Ascendente'
   AND f.IdTipoPersonal = jefe.IdTipoPersonal
WHERE e.Estado = 'Activo'
  AND tpJefe.PermiteEvaluacionAscendente = 1;
GO

-- =========================================================
-- 8. CASO DE EJEMPLO RESUELTO
--    Se completa una evaluación real (autoevaluación + evaluación
--    del jefe) para el operario Wilson Alberto Gómez (1013),
--    evaluado por su jefe Laura Fernanda Rojas (1005), y se
--    consolida el resultado del periodo.
-- =========================================================

DECLARE @IdPeriodo2026 INT = (SELECT IdPeriodo FROM dbo.PeriodoEvaluacion WHERE Nombre = N'Evaluación de Desempeño 2026');

DECLARE @IdAsigAuto INT = (
    SELECT IdAsignacion FROM dbo.AsignacionEvaluacion
    WHERE IdPeriodo = @IdPeriodo2026 AND CodigoEvaluador = 1013 AND CodigoEvaluado = 1013 AND TipoRelacion = 'Autoevaluacion'
);
DECLARE @IdAsigJefe INT = (
    SELECT IdAsignacion FROM dbo.AsignacionEvaluacion
    WHERE IdPeriodo = @IdPeriodo2026 AND CodigoEvaluador = 1005 AND CodigoEvaluado = 1013 AND TipoRelacion = 'Jefe'
);

INSERT INTO dbo.RespuestaEvaluacion (IdAsignacion, FechaEnvio, Estado) VALUES (@IdAsigAuto, SYSDATETIME(), 'Enviada');
INSERT INTO dbo.RespuestaEvaluacion (IdAsignacion, FechaEnvio, Estado) VALUES (@IdAsigJefe, SYSDATETIME(), 'Enviada');

DECLARE @IdRespAuto INT = (SELECT IdRespuesta FROM dbo.RespuestaEvaluacion WHERE IdAsignacion = @IdAsigAuto);
DECLARE @IdRespJefe INT = (SELECT IdRespuesta FROM dbo.RespuestaEvaluacion WHERE IdAsignacion = @IdAsigJefe);

DECLARE @IdFormAutoOperario INT = (
    SELECT f.IdFormulario FROM dbo.FormularioEvaluacion f
    JOIN dbo.TipoPersonal tp ON tp.IdTipoPersonal = f.IdTipoPersonal
    WHERE f.IdPeriodo = @IdPeriodo2026 AND f.TipoRelacion = 'Autoevaluacion' AND tp.Nombre = N'Operario'
);
DECLARE @IdFormJefeOperario INT = (
    SELECT f.IdFormulario FROM dbo.FormularioEvaluacion f
    JOIN dbo.TipoPersonal tp ON tp.IdTipoPersonal = f.IdTipoPersonal
    WHERE f.IdPeriodo = @IdPeriodo2026 AND f.TipoRelacion = 'Jefe' AND tp.Nombre = N'Operario'
);

-- Autoevaluación: Wilson se califica con 85% en todas sus competencias (Entregable 12 — escala
-- nativa 0-100%, ya no 1-5; 85% cae en la banda "Bueno" de EscalaCalificacion).
--
-- Entregable 13 — comportamientos: por simplicidad del ejemplo, se califica CADA comportamiento
-- de cada competencia con el mismo valor (85.00 aquí, 60.00 más abajo para el jefe), en vez de
-- variar comportamiento por comportamiento — así el promedio por competencia (=AVERAGE(...), lo
-- que en la aplicación calcula el servidor al guardar) da exactamente 85.00/60.00, reproduciendo
-- el mismo resultado que ya estaba verificado con el harness offline antes de agregar la capa de
-- comportamientos (ver Entregable 12), sin tener que recalcular el caso de ejemplo a mano.
INSERT INTO dbo.RespuestaDetalle (IdRespuesta, IdCompetencia, Calificacion, Comentario)
SELECT @IdRespAuto, fc.IdCompetencia, 85.00, N'Autoevaluación — ejemplo de datos ficticios'
FROM dbo.FormularioCompetencia fc
WHERE fc.IdFormulario = @IdFormAutoOperario;

INSERT INTO dbo.RespuestaComportamientoDetalle (IdRespuesta, IdComportamiento, Calificacion)
SELECT @IdRespAuto, co.IdComportamiento, 85.00
FROM dbo.FormularioCompetencia fc
JOIN dbo.Comportamiento co ON co.IdCompetencia = fc.IdCompetencia
WHERE fc.IdFormulario = @IdFormAutoOperario;

-- Evaluación del jefe: Laura califica a Wilson con 60% en todas sus competencias. Nota: el
-- PromedioJefe consolidado más abajo NO queda en 60% porque combina esto con los indicadores de
-- gestión (que pesan más dentro del formulario y tienen valores altos, 78-95%) — el resultado
-- verificado con un harness aparte (fuera de este script) da ~73.75%, banda "Aceptable" de
-- EscalaCalificacion, todavía claramente por debajo del ~86.25% de la autoevaluación. (Valores
-- actualizados en el Entregable 14 tras normalizar el peso de los indicadores de gestión dentro
-- de su grupo — antes de esa corrección daban ~75.7%/~86.4%, con el grupo de indicadores pesando
-- efectivamente ~66.7% en vez de su 50% nominal.)
INSERT INTO dbo.RespuestaDetalle (IdRespuesta, IdCompetencia, Calificacion, Comentario)
SELECT @IdRespJefe, fc.IdCompetencia, 60.00, N'Evaluación del jefe — ejemplo de datos ficticios'
FROM dbo.FormularioCompetencia fc
WHERE fc.IdFormulario = @IdFormJefeOperario;

INSERT INTO dbo.RespuestaComportamientoDetalle (IdRespuesta, IdComportamiento, Calificacion)
SELECT @IdRespJefe, co.IdComportamiento, 60.00
FROM dbo.FormularioCompetencia fc
JOIN dbo.Comportamiento co ON co.IdCompetencia = fc.IdCompetencia
WHERE fc.IdFormulario = @IdFormJefeOperario;

-- Indicadores de gestión (Entregable 11) — Meta y Resultado del mes son datos operativos
-- objetivos del área, no una apreciación subjetiva del evaluador; por eso se usan los mismos
-- valores de ejemplo (ficticios) tanto en la autoevaluación como en la evaluación del jefe. Meta
-- se toma del valor fijo del catálogo (dbo.IndicadorGestion.Meta, Entregable 12) — no se inventa
-- un valor distinto aquí, igual que hace el controlador de la aplicación al guardar.
INSERT INTO dbo.RespuestaIndicadorDetalle (IdRespuesta, IdIndicador, Meta, ResultadoMes)
SELECT @IdRespAuto, i.IdIndicador, i.Meta,
       CASE i.Nombre
           WHEN N'Cultura: 5S+1' THEN 95.00
           WHEN N'Eficiencia'    THEN 85.00
           WHEN N'Calidad'       THEN 92.00
           WHEN N'Ausentismo'    THEN 78.00
       END
FROM dbo.FormularioIndicador fi
JOIN dbo.IndicadorGestion i ON i.IdIndicador = fi.IdIndicador
WHERE fi.IdFormulario = @IdFormAutoOperario;

INSERT INTO dbo.RespuestaIndicadorDetalle (IdRespuesta, IdIndicador, Meta, ResultadoMes)
SELECT @IdRespJefe, i.IdIndicador, i.Meta,
       CASE i.Nombre
           WHEN N'Cultura: 5S+1' THEN 95.00
           WHEN N'Eficiencia'    THEN 85.00
           WHEN N'Calidad'       THEN 92.00
           WHEN N'Ausentismo'    THEN 78.00
       END
FROM dbo.FormularioIndicador fi
JOIN dbo.IndicadorGestion i ON i.IdIndicador = fi.IdIndicador
WHERE fi.IdFormulario = @IdFormJefeOperario;

UPDATE dbo.AsignacionEvaluacion SET Estado = 'Completada' WHERE IdAsignacion IN (@IdAsigAuto, @IdAsigJefe);

-- Promedio ponderado por la Ponderacion real de cada ítem dentro de su formulario (RF-06/RF-07
-- ampliado en el Entregable 11 — sección 6), combinando competencias e indicadores de gestión.
-- Desde el Entregable 12 ambos tipos de ítem ya están en la misma escala nativa % (0-100), así
-- que se combinan DIRECTAMENTE (Σ valor×peso / Σ peso), sin ninguna conversión de escala
-- intermedia (antes había una equivalencia a escala 1-5 aquí; ver ResultadoService.PromedioPonderado
-- en la aplicación, que sigue exactamente esta misma fórmula simplificada). La suma se
-- autonormaliza dividiendo por SUM(Ponderacion) de todos los ítems presentes (no un denominador
-- fijo de 100), por lo que el resultado queda siempre acotado en [0,100]. Desde el Entregable 14,
-- el peso de cada indicador dentro de FormularioIndicador.Ponderacion ya viene normalizado en la
-- sección 6 de este script para que el grupo "IndicadoresGestion" siempre sume su 50% nominal
-- (aunque la Ponderacion del catálogo, columna 5, siga sumando ~133% entre los cuatro indicadores
-- — ver sección 5). Para el formulario de Operario usado en este ejemplo
-- las competencias no tienen categoría configurada como caso especial, pero sí están presentes
-- las tres claves de macro-grupo (Organizacional/DeRol/IndicadoresGestion), así que se aplican
-- los pesos fijos 20%/30%/50% de la sección 6.
INSERT INTO dbo.ResultadoConsolidado (CodigoEvaluado, IdPeriodo, PromedioAutoevaluacion, PromedioJefe, PromedioAscendente, PromedioGeneral, FechaConsolidacion)
SELECT
    1013,
    @IdPeriodo2026,
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT rd.Calificacion AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormAutoOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespAuto
        UNION ALL
        SELECT rid.ResultadoMes, fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormAutoOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespAuto AND rid.ResultadoMes IS NOT NULL
     ) AS x),
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT rd.Calificacion AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormJefeOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespJefe
        UNION ALL
        SELECT rid.ResultadoMes, fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormJefeOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespJefe AND rid.ResultadoMes IS NOT NULL
     ) AS x),
    NULL,
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT rd.Calificacion AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormAutoOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespAuto
        UNION ALL
        SELECT rd.Calificacion, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormJefeOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespJefe
        UNION ALL
        SELECT rid.ResultadoMes, fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormAutoOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespAuto AND rid.ResultadoMes IS NOT NULL
        UNION ALL
        SELECT rid.ResultadoMes, fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormJefeOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespJefe AND rid.ResultadoMes IS NOT NULL
     ) AS x),
    SYSDATETIME();
GO

-- =========================================================
-- 9. CONSULTAS DE VERIFICACIÓN / EJEMPLOS DE REPORTE
-- =========================================================

-- 9.1 Conteo de filas por tabla (verificación rápida de la carga)
SELECT 'TipoPersonal' AS Tabla, COUNT(*) AS Filas FROM dbo.TipoPersonal
UNION ALL SELECT 'Empleado', COUNT(*) FROM dbo.Empleado
UNION ALL SELECT 'Rol', COUNT(*) FROM dbo.Rol
UNION ALL SELECT 'Usuario', COUNT(*) FROM dbo.Usuario
UNION ALL SELECT 'UsuarioRol', COUNT(*) FROM dbo.UsuarioRol
UNION ALL SELECT 'Competencia', COUNT(*) FROM dbo.Competencia
UNION ALL SELECT 'Comportamiento', COUNT(*) FROM dbo.Comportamiento
UNION ALL SELECT 'IndicadorGestion', COUNT(*) FROM dbo.IndicadorGestion
UNION ALL SELECT 'PeriodoEvaluacion', COUNT(*) FROM dbo.PeriodoEvaluacion
UNION ALL SELECT 'FormularioEvaluacion', COUNT(*) FROM dbo.FormularioEvaluacion
UNION ALL SELECT 'FormularioCompetencia', COUNT(*) FROM dbo.FormularioCompetencia
UNION ALL SELECT 'FormularioIndicador', COUNT(*) FROM dbo.FormularioIndicador
UNION ALL SELECT 'AsignacionEvaluacion', COUNT(*) FROM dbo.AsignacionEvaluacion
UNION ALL SELECT 'RespuestaEvaluacion', COUNT(*) FROM dbo.RespuestaEvaluacion
UNION ALL SELECT 'RespuestaDetalle', COUNT(*) FROM dbo.RespuestaDetalle
UNION ALL SELECT 'RespuestaComportamientoDetalle', COUNT(*) FROM dbo.RespuestaComportamientoDetalle
UNION ALL SELECT 'RespuestaIndicadorDetalle', COUNT(*) FROM dbo.RespuestaIndicadorDetalle
UNION ALL SELECT 'ResultadoConsolidado', COUNT(*) FROM dbo.ResultadoConsolidado
UNION ALL SELECT 'ContactoNotificacion', COUNT(*) FROM dbo.ContactoNotificacion
UNION ALL SELECT 'EnvioResultadoToken', COUNT(*) FROM dbo.EnvioResultadoToken;
GO

-- 9.2 Organigrama plano: empleado, tipo de personal y jefe directo
SELECT
    e.CodigoEmpleado, e.Nombre, e.Cargo, tp.Nombre AS TipoPersonal,
    jefe.Nombre AS JefeDirecto, e.Area
FROM dbo.Empleado e
JOIN dbo.TipoPersonal tp ON tp.IdTipoPersonal = e.IdTipoPersonal
LEFT JOIN dbo.Empleado jefe ON jefe.CodigoEmpleado = e.CodigoJefeDirecto
ORDER BY tp.IdTipoPersonal, e.Nombre;
GO

-- 9.3 Avance del periodo 2026: evaluaciones completadas vs. pendientes por área
SELECT
    e.Area,
    COUNT(*) AS TotalAsignadas,
    SUM(CASE WHEN a.Estado = 'Completada' THEN 1 ELSE 0 END) AS Completadas,
    SUM(CASE WHEN a.Estado <> 'Completada' THEN 1 ELSE 0 END) AS Pendientes
FROM dbo.AsignacionEvaluacion a
JOIN dbo.Empleado e ON e.CodigoEmpleado = a.CodigoEvaluado
JOIN dbo.PeriodoEvaluacion p ON p.IdPeriodo = a.IdPeriodo
WHERE p.Nombre = N'Evaluación de Desempeño 2026'
GROUP BY e.Area
ORDER BY e.Area;
GO

-- 9.4 Resultado consolidado disponible, con datos del empleado
SELECT
    e.Nombre, e.Cargo, tp.Nombre AS TipoPersonal,
    rc.PromedioAutoevaluacion, rc.PromedioJefe, rc.PromedioAscendente, rc.PromedioGeneral
FROM dbo.ResultadoConsolidado rc
JOIN dbo.Empleado e ON e.CodigoEmpleado = rc.CodigoEvaluado
JOIN dbo.TipoPersonal tp ON tp.IdTipoPersonal = e.IdTipoPersonal
JOIN dbo.PeriodoEvaluacion p ON p.IdPeriodo = rc.IdPeriodo
WHERE p.Nombre = N'Evaluación de Desempeño 2026';
GO
