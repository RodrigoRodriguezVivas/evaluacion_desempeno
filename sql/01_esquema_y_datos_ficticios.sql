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

-- ---- Indicadores de gestión (Entregable 11 — macro-grupo "Indicadores de Gestión", formato
-- real "EVALUACION DESEMPEÑO Indicadores" de Alianzagrafica). A diferencia de Competencia (que
-- el evaluador califica de 1 a 5), un indicador se mide con una Meta y un Resultado del mes, en
-- puntos porcentuales — ver dbo.RespuestaIndicadorDetalle más abajo.
CREATE TABLE dbo.IndicadorGestion (
    IdIndicador    INT IDENTITY(1,1)  NOT NULL,
    Nombre         NVARCHAR(150)      NOT NULL,
    Formula        NVARCHAR(400)      NULL,           -- descripción de la fórmula/definición del indicador
    Ponderacion    DECIMAL(6,3)       NOT NULL,        -- peso DENTRO del grupo (no del total), en % (ej. 33.33)
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

CREATE TABLE dbo.RespuestaDetalle (
    IdRespuesta   INT           NOT NULL,
    IdCompetencia INT           NOT NULL,
    Calificacion  TINYINT       NOT NULL,
    Comentario    NVARCHAR(500) NULL,
    CONSTRAINT PK_RespuestaDetalle PRIMARY KEY (IdRespuesta, IdCompetencia),
    CONSTRAINT FK_Detalle_Respuesta FOREIGN KEY (IdRespuesta) REFERENCES dbo.RespuestaEvaluacion (IdRespuesta),
    CONSTRAINT FK_Detalle_Competencia FOREIGN KEY (IdCompetencia) REFERENCES dbo.Competencia (IdCompetencia),
    CONSTRAINT CK_Detalle_Calificacion CHECK (Calificacion BETWEEN 1 AND 5)
);
GO

-- Entregable 11: respuesta de un indicador de gestión — Meta y Resultado del mes en % en vez de
-- una calificación de 1 a 5 (ver dbo.IndicadorGestion).
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
CREATE TABLE dbo.ResultadoConsolidado (
    CodigoEvaluado          INT           NOT NULL,
    IdPeriodo               INT           NOT NULL,
    PromedioAutoevaluacion  DECIMAL(4,2)  NULL,
    PromedioJefe            DECIMAL(4,2)  NULL,
    PromedioAscendente      DECIMAL(4,2)  NULL,
    PromedioGeneral         DECIMAL(4,2)  NULL,
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
-- 5. COMPETENCIAS (propuesta inicial — sección 5.2 del documento)
-- =========================================================

-- Genéricas (IdTipoPersonal = NULL, aplican a todo el personal). Categoria = 'Organizacional'
-- (RF-07 — macro-grupo "EVALUACION DE COMPETENCIAS ORGANIZACIONALES", peso 20% del total desde
-- el Entregable 11 —antes 50%—, según el formato real GHU-FOR-007 / "EVALUACION DESEMPEÑO
-- Indicadores" de Alianzagrafica — ver Entregable 5 y los ajustes posteriores de
-- agrupación/ponderación).
INSERT INTO dbo.Competencia (Nombre, IdTipoPersonal, Categoria) VALUES
    (N'Compromiso y responsabilidad', NULL, N'Organizacional'),
    (N'Trabajo en equipo', NULL, N'Organizacional'),
    (N'Comunicación', NULL, N'Organizacional'),
    (N'Adaptación al cambio', NULL, N'Organizacional'),
    (N'Cumplimiento de normas de seguridad industrial y calidad', NULL, N'Organizacional');
GO

-- Específicas por tipo de personal. Categoria = 'DeRol' (macro-grupo "EVALUACION DE
-- COMPETENCIAS DE ROL", peso 30% del total desde el Entregable 11 —antes 50%—).
INSERT INTO dbo.Competencia (Nombre, IdTipoPersonal, Categoria)
SELECT N'Liderazgo y desarrollo de personas', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Directivo'
UNION ALL SELECT N'Visión estratégica', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Directivo'
UNION ALL SELECT N'Toma de decisiones', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Directivo'
UNION ALL SELECT N'Gestión del cambio', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Directivo'
UNION ALL SELECT N'Orientación a resultados', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Directivo'

UNION ALL SELECT N'Liderazgo de equipos', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Mando medio'
UNION ALL SELECT N'Planificación y organización', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Mando medio'
UNION ALL SELECT N'Resolución de problemas', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Mando medio'
UNION ALL SELECT N'Gestión de indicadores de área', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Mando medio'

UNION ALL SELECT N'Orientación al servicio interno', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Administrativo'
UNION ALL SELECT N'Precisión y manejo de detalle', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Administrativo'
UNION ALL SELECT N'Manejo de herramientas ofimáticas/ERP', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Administrativo'
UNION ALL SELECT N'Gestión del tiempo', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Administrativo'

UNION ALL SELECT N'Calidad y precisión en el proceso productivo', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Operario'
UNION ALL SELECT N'Manejo de maquinaria y equipos', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Operario'
UNION ALL SELECT N'Cumplimiento de estándares de producción', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Operario'
UNION ALL SELECT N'Seguridad y orden en el puesto de trabajo', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Operario'

UNION ALL SELECT N'Disposición y colaboración', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Auxiliar de planta'
UNION ALL SELECT N'Cumplimiento de instrucciones', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Auxiliar de planta'
UNION ALL SELECT N'Orden y aseo', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Auxiliar de planta'
UNION ALL SELECT N'Seguridad industrial', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Auxiliar de planta'

-- Conductor — competencias de rol tomadas del formato real GHU-FOR-007 de Alianzagrafica,
-- sección "COMPETENCIAS DE ROL" (mismo enriquecimiento aplicado en la demo — ver Entregable 5).
-- "Trabajo en equipo" tiene aquí su propia versión específica del Conductor, con comportamientos
-- de coordinación con el equipo de despachos, DISTINTA de la genérica "Organizacional" de arriba
-- — la consulta de ponderación de la sección 6 hace que la específica reemplace a la genérica
-- del mismo nombre para este tipo de personal, igual que en AsignacionService (aplicación).
UNION ALL SELECT N'Orientación al cliente', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor'
UNION ALL SELECT N'Trabajo en equipo', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor'
UNION ALL SELECT N'Orientación al logro', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor'
UNION ALL SELECT N'Atención al detalle', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor'
UNION ALL SELECT N'Sentido de la urgencia', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor'
UNION ALL SELECT N'Escucha activa', IdTipoPersonal, N'DeRol' FROM dbo.TipoPersonal WHERE Nombre = N'Conductor';
GO

-- Indicadores de gestión (Entregable 11 — macro-grupo "INDICADORES DE GESTIÓN", peso 50% del
-- total, según el formato real "EVALUACION DESEMPEÑO Indicadores" de Alianzagrafica). Genéricos
-- (IdTipoPersonal = NULL, aplican a todo el personal), igual que en DemoSeed.cs (aplicación).
-- Las ponderaciones (33.33% cada uno) están tal como figuran en el Excel origen y, junto con las
-- otras tres filas, suman ~133.33% dentro del grupo — decisión explícita del usuario (mantener
-- exactamente igual que el Excel, sin normalizar), ver Constantes.PesoIndicadoresGestion y la
-- nota en la sección 6 de este script.
INSERT INTO dbo.IndicadorGestion (Nombre, Formula, Ponderacion, IdTipoPersonal) VALUES
    (N'Cultura: 5S+1', N'Costo de reclamos del cliente ($) facturación.', 33.33, NULL),
    (N'Eficiencia', N'Cantidad unidades defectuosas / Cantidad unidades producidas', 33.33, NULL),
    (N'Calidad', N'(Horas laboradas - Horas de ausentismo) / Horas totales laboradas', 33.33, NULL),
    (N'Ausentismo', N'Rendimiento real / Rendimiento esperado', 33.33, NULL);
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
-- parejo entre indicadores, a diferencia de las competencias. Igual que
-- AsignacionService.GenerarFormulariosAsync (pesoAbsoluto = pesoGrupoIndicadores * Ponderacion/100).
INSERT INTO dbo.FormularioIndicador (IdFormulario, IdIndicador, Ponderacion)
SELECT f.IdFormulario, i.IdIndicador,
       CAST(pg.PesoGrupo * (i.Ponderacion / 100.0) AS DECIMAL(5,2))
FROM dbo.FormularioEvaluacion f
JOIN dbo.IndicadorGestion i
    ON i.Activa = 1
   AND (i.IdTipoPersonal = f.IdTipoPersonal OR i.IdTipoPersonal IS NULL)
JOIN @PesoPorGrupo pg ON pg.IdFormulario = f.IdFormulario AND pg.ClaveGrupo = N'IndicadoresGestion';
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

-- Autoevaluación: Wilson se califica con 4 en todas sus competencias
INSERT INTO dbo.RespuestaDetalle (IdRespuesta, IdCompetencia, Calificacion, Comentario)
SELECT @IdRespAuto, fc.IdCompetencia, 4, N'Autoevaluación — ejemplo de datos ficticios'
FROM dbo.FormularioCompetencia fc
WHERE fc.IdFormulario = @IdFormAutoOperario;

-- Evaluación del jefe: Laura califica a Wilson con 3 en todas sus competencias
INSERT INTO dbo.RespuestaDetalle (IdRespuesta, IdCompetencia, Calificacion, Comentario)
SELECT @IdRespJefe, fc.IdCompetencia, 3, N'Evaluación del jefe — ejemplo de datos ficticios'
FROM dbo.FormularioCompetencia fc
WHERE fc.IdFormulario = @IdFormJefeOperario;

-- Indicadores de gestión (Entregable 11) — Meta y Resultado del mes son datos operativos
-- objetivos del área, no una apreciación subjetiva del evaluador; por eso se usan los mismos
-- valores de ejemplo (ficticios) tanto en la autoevaluación como en la evaluación del jefe.
INSERT INTO dbo.RespuestaIndicadorDetalle (IdRespuesta, IdIndicador, Meta, ResultadoMes)
SELECT @IdRespAuto, fi.IdIndicador, 90.00,
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
SELECT @IdRespJefe, fi.IdIndicador, 90.00,
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
-- ampliado en el Entregable 11 — sección 6), combinando competencias (escala 1-5 directa) e
-- indicadores de gestión (Meta/Resultado del mes en %, convertidos a escala 1-5 mediante la
-- misma fórmula que ResultadoService.EquivalenciaUnoACinco de la aplicación:
-- CLAMP(1 + 4*(ResultadoMes/100), 1, 5) — 0%→1, 100%→5, acotado para no desbordar la escala
-- combinada). La suma se autonormaliza dividiendo por SUM(Ponderacion) de todos los ítems
-- presentes (no un denominador fijo de 100), por lo que el resultado queda siempre acotado en
-- [1,5] aun cuando las ponderaciones de los indicadores dentro de su grupo sumen ~133% (decisión
-- explícita del usuario, ver sección 5 de este script). Para el formulario de Operario usado en
-- este ejemplo las competencias no tienen categoría configurada como caso especial, pero sí
-- están presentes las tres claves de macro-grupo (Organizacional/DeRol/IndicadoresGestion), así
-- que se aplican los pesos fijos 20%/30%/50% de la sección 6.
INSERT INTO dbo.ResultadoConsolidado (CodigoEvaluado, IdPeriodo, PromedioAutoevaluacion, PromedioJefe, PromedioAscendente, PromedioGeneral, FechaConsolidacion)
SELECT
    1013,
    @IdPeriodo2026,
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT CAST(rd.Calificacion AS DECIMAL(9,4)) AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormAutoOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespAuto
        UNION ALL
        SELECT
            CASE WHEN 1 + 4 * (rid.ResultadoMes / 100.0) < 1 THEN 1
                 WHEN 1 + 4 * (rid.ResultadoMes / 100.0) > 5 THEN 5
                 ELSE 1 + 4 * (rid.ResultadoMes / 100.0) END,
            fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormAutoOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespAuto AND rid.ResultadoMes IS NOT NULL
     ) AS x),
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT CAST(rd.Calificacion AS DECIMAL(9,4)) AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormJefeOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespJefe
        UNION ALL
        SELECT
            CASE WHEN 1 + 4 * (rid.ResultadoMes / 100.0) < 1 THEN 1
                 WHEN 1 + 4 * (rid.ResultadoMes / 100.0) > 5 THEN 5
                 ELSE 1 + 4 * (rid.ResultadoMes / 100.0) END,
            fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormJefeOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespJefe AND rid.ResultadoMes IS NOT NULL
     ) AS x),
    NULL,
    (SELECT SUM(x.Valor * x.Ponderacion) / NULLIF(SUM(x.Ponderacion), 0)
     FROM (
        SELECT CAST(rd.Calificacion AS DECIMAL(9,4)) AS Valor, fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormAutoOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespAuto
        UNION ALL
        SELECT CAST(rd.Calificacion AS DECIMAL(9,4)), fc.Ponderacion
        FROM dbo.RespuestaDetalle rd
        JOIN dbo.FormularioCompetencia fc ON fc.IdFormulario = @IdFormJefeOperario AND fc.IdCompetencia = rd.IdCompetencia
        WHERE rd.IdRespuesta = @IdRespJefe
        UNION ALL
        SELECT
            CASE WHEN 1 + 4 * (rid.ResultadoMes / 100.0) < 1 THEN 1
                 WHEN 1 + 4 * (rid.ResultadoMes / 100.0) > 5 THEN 5
                 ELSE 1 + 4 * (rid.ResultadoMes / 100.0) END,
            fi.Ponderacion
        FROM dbo.RespuestaIndicadorDetalle rid
        JOIN dbo.FormularioIndicador fi ON fi.IdFormulario = @IdFormAutoOperario AND fi.IdIndicador = rid.IdIndicador
        WHERE rid.IdRespuesta = @IdRespAuto AND rid.ResultadoMes IS NOT NULL
        UNION ALL
        SELECT
            CASE WHEN 1 + 4 * (rid.ResultadoMes / 100.0) < 1 THEN 1
                 WHEN 1 + 4 * (rid.ResultadoMes / 100.0) > 5 THEN 5
                 ELSE 1 + 4 * (rid.ResultadoMes / 100.0) END,
            fi.Ponderacion
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
UNION ALL SELECT 'IndicadorGestion', COUNT(*) FROM dbo.IndicadorGestion
UNION ALL SELECT 'PeriodoEvaluacion', COUNT(*) FROM dbo.PeriodoEvaluacion
UNION ALL SELECT 'FormularioEvaluacion', COUNT(*) FROM dbo.FormularioEvaluacion
UNION ALL SELECT 'FormularioCompetencia', COUNT(*) FROM dbo.FormularioCompetencia
UNION ALL SELECT 'FormularioIndicador', COUNT(*) FROM dbo.FormularioIndicador
UNION ALL SELECT 'AsignacionEvaluacion', COUNT(*) FROM dbo.AsignacionEvaluacion
UNION ALL SELECT 'RespuestaEvaluacion', COUNT(*) FROM dbo.RespuestaEvaluacion
UNION ALL SELECT 'RespuestaDetalle', COUNT(*) FROM dbo.RespuestaDetalle
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
